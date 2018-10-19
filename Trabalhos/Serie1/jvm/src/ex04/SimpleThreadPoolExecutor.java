package ex04;

import ex04.utils.TimeoutHolder;

import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class SimpleThreadPoolExecutor {

    private static class WorkerThread  {

        private static final Lock lock = new ReentrantLock();

        private long keepAliveUntil;
//        private Runnable action;
        private boolean executed;
        private Thread thread;

//        private WorkerThread(Runnable action, int keepAliveTime) {
        private WorkerThread(int keepAliveTime) {
//            this.action = action;

            executed = false;

            lock.lock();
            long now;

            try {
                now = System.currentTimeMillis();
                keepAliveUntil = now + keepAliveTime;
            } finally {
                lock.unlock();
            }
        }

        public boolean expired(){
            return !thread.isAlive() && System.currentTimeMillis() <= keepAliveUntil;
        }

        public void run(Runnable action) {
            if(action == null){
                throw new IllegalArgumentException();
            }

            executed = false;

            try {
                thread = new Thread( () -> {
                    try {
                        action.run();
                    }
                    finally {
                        executed = true;
                    }
                });
                thread.start();
            }
            catch (Exception e) {
                // ignored
            }
        }
    }

    private final Object _monitor;

    private enum PoolState {Active, InShutDown, Terminated}

    private PoolState _state;

    private LinkedList<WorkerThread> workerThreads;

    private int _maxPoolSize;
    private int _keepAliveTime;

    public SimpleThreadPoolExecutor(int maxPoolSize, int keepAliveTime){
        _monitor = new Object();
        workerThreads =  new LinkedList<>();

        _state = PoolState.Active;

        this._maxPoolSize = maxPoolSize;
        this._keepAliveTime = keepAliveTime;
    }

//    (1) se o número total de worker threads for inferior ao limite máximo especificado, é criada uma nova worker thread sempre que for
//        submetido um comando para execução e não existir nenhuma worker thread disponível;
//    (2) as worker threads deverão terminar após decorrerem mais do que k​eepAliveTime milésimos de
//        segundo sem que sejam mobilizadas para executar um comando;
//    (3) o número de worker threads existentes no pool em cada momento depende da actividade deste e pode variar entre zero e m​axPoolSize​.
//          As threads que pretendem executar funções através do thread pool executor invocam o método execute, especificando o comando a executar com o argumento command.
//          Este método pode bloquear a thread invocante, pois tem que garantir que o comando especificado
//          foi entregue a uma worker threads para execução, e pode terminar:
//            (a) normalmente, devolvendo true, se o comando foi entregue para execução;
//            (b) excepcionalmente, lançando a excepção RejectedExecutionException, se o thread pool se encontrar em modo shutting down;
//            (c) excepcionalmente, devolvendo false, se expirar o limite de tempo especificado com timeout sem que o comando seja entregue a uma worker thread, ou;
//            (d) excepcionalmente, lançando InterruptedException, se o bloqueio da thread for interrompido.

    public boolean execute(Runnable command, int timeout) throws InterruptedException, RejectedExecutionException
    {
        WorkerThread workerThread = null;

        synchronized (_monitor) {
            // Neste modo, todas as chamadas ao método e​xecute deverão lançar a excepção RejectedExecutionException​.
            if (_state != PoolState.Active) {
                throw new RejectedExecutionException();
            }

            // Contudo, todas as submissões para execução feitas antes da chamada ao método s​hutdown devem ser processadas normalmente.
            if (timeout == 0) {
                return false;
            }

            TimeoutHolder th = new TimeoutHolder(timeout);

            List<WorkerThread> cleanUpExpired = new ArrayList<>();

            try {
                do {
                    for (WorkerThread wt : workerThreads) { // reuse thread
                        if (wt.expired()) {
                            cleanUpExpired.add(wt);
                        } else if (wt.executed) {
                            wt.executed = false;
                            workerThread = wt;
                            break;
                        }
                    }

                    if(cleanUpExpired.size() > 0){
                        for (WorkerThread expiredWt : cleanUpExpired){
                            workerThreads.remove(expiredWt);
                        }
                    }

                    if(_state == PoolState.InShutDown) {
                        _monitor.notifyAll(); // to wake up awaitTermination and other pending threads
                    }

                    if(workerThread != null){
                        break;
                    }

                    if (workerThreads.size() < _maxPoolSize) { // create new thread
                        workerThread = new WorkerThread(_keepAliveTime);
                        workerThreads.addLast(workerThread);
                        break;
                    }

                    if (th.timeout()) {
                        return false;
                    }

                    _monitor.wait(timeout);

                    if(_state != PoolState.Active){
                        throw new RejectedExecutionException();
                    }

                } while (true);
            }
            catch (InterruptedException e){
                throw e;
            }
        }

        workerThread.run(command);

        return true;
    }

// O método awaitTermination permite à thread invocante sincronizar-se com a conclusão do processo de shutdown do executor,
// isto é, até que sejam executados todos os comandos aceites e que todas as worker threads activas terminem, e pode terminar:
//        (a) normalmente, devolvendo true​, quando o shutdown do executor estiver concluído;
//        (b) excepcionalmente, devolvendo false​, se expirar o limite de tempo especificado com o argumento timeout​, sem que o shutdown termine, ou;
//        (c) excepcionalmente, lançando InterruptedException​, se o bloqueio da thread for interrompido.

    public boolean awaitTermination(int timeout) throws InterruptedException, RejectedExecutionException {
        synchronized (_monitor){

            if(_state == PoolState.InShutDown && workerThreads.size() == 0){
                _state = PoolState.Terminated;
                return true;
            }

            if(timeout == 0){
                return false;
            }

            TimeoutHolder th = new TimeoutHolder(timeout);

            try{
                do{
                    _monitor.wait(timeout);

                    boolean allExecuted = true;

                    for(WorkerThread wt : workerThreads){
                        allExecuted &= wt.executed;
                    }

                    if(allExecuted){ // workerThreads.size() == 0
                        _state = PoolState.Terminated;
                        return true;
                    }
                    if(th.timeout()){
                        return false;
                    }
                }
                while (true);
            }
            catch (InterruptedException e){
                if(workerThreads.size() == 0){
                    _state = PoolState.Terminated;
                    return true;
                }

                throw e;
            }
        }
    }

// A chamada ao método s​hutdown coloca o executor em modo shutting down e retorna de imediato.
    public void shutdown() throws IllegalStateException
    {
        synchronized (_monitor){
            if(_state != PoolState.Active){
                throw new IllegalStateException(); // TODO REVIEW
            }

            _state = PoolState.InShutDown;

            _monitor.notifyAll();
        }
    }
}