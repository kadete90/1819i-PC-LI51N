package tests

import ex04.SimpleThreadPoolExecutor
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test

import java.util.concurrent.RejectedExecutionException

import static org.junit.jupiter.api.Assertions.assertEquals
import static org.junit.jupiter.api.Assertions.assertFalse
import static org.junit.jupiter.api.Assertions.assertTrue

import java.util.concurrent.atomic.AtomicLong

class SimpleThreadPoolExecutorTest {

    @Test
    void testAllSuccess() {

        AtomicLong counter = new AtomicLong()

        int nrMaxOfWorkerThreads = 10
        int nrOfExecutes = 20

        int second = 1000
        int keepAliveTime = 20 * second
        int timeOut = 3 * second

        SimpleThreadPoolExecutor executor = new SimpleThreadPoolExecutor(nrMaxOfWorkerThreads, keepAliveTime);

        assertTrue(counter.get() == 0)

        for(int i = 0; i < nrOfExecutes; i++){
            assertTrue(executor.execute(
                    new Runnable() {
                        void run() {
                            Thread.sleep(second)
                            System.out.println("testAllSuccess.MyRunnable '" + counter.incrementAndGet() + "' is running...")
                        }
                    }, timeOut))
        }

        executor.shutdown()

        assertTrue(executor.awaitTermination(5*second)) // enought time to run 2 batches

        assertEquals(nrOfExecutes, counter.get())
    }

    @Test
    void testRunDeliveredBeforeShutDown() {
        AtomicLong counterT2 = new AtomicLong()

        int nrMaxOfWorkerThreads = 10
        int nrOfExecutes = 20

        int second = 1000
        int keepAliveTime = 20 * second
        int timeOut = 1 * second

        SimpleThreadPoolExecutor executor = new SimpleThreadPoolExecutor(nrMaxOfWorkerThreads, keepAliveTime);

        assertTrue(counterT2.get() == 0)

        for(int i = 0; i < nrOfExecutes; i++){
            executor.execute(
                    new Runnable() {
                        void run() {
                            Thread.sleep(second)
                            System.out.println("testRunDeliveredBeforeShutDown.MyRunnable '" + counterT2.incrementAndGet() + "' is running...")
                        }
                    }, timeOut)
        }

        executor.shutdown()

        assertFalse(executor.awaitTermination((int)(second / 2)))
        //the threads that were previous submitted before shutdown+awaitTermination return success
        assertEquals(nrMaxOfWorkerThreads, counterT2.get())
    }

    @Test
    void testRejectedExecutionException() {
        AtomicLong counterT2 = new AtomicLong()

        int nrMaxOfWorkerThreads = 10
        int nrOfExecutes = 20

        int second = 1000
        int keepAliveTime = 20 * second
        int timeOut = 1 * second

        SimpleThreadPoolExecutor executor = new SimpleThreadPoolExecutor(nrMaxOfWorkerThreads, keepAliveTime);

        assertTrue(counterT2.get() == 0)

        for(int i = 0; i < nrOfExecutes; i++){
            executor.execute(
                    new Runnable() {
                        void run() {
                            Thread.sleep(second)
                            System.out.println("testRejectedExecutionException.MyRunnable '" + counterT2.incrementAndGet() + "' is running...")
                        }
                    }, timeOut)
        }

        executor.shutdown()

        try {
            executor.execute(
                    new Runnable() {
                        void run() {
                            Thread.sleep(second)
                            System.out.println("testRejectedExecutionException.MyRunnable '" + counterT2.incrementAndGet() + "' is running...")
                        }
                    }, timeOut)

            assertTrue(false)
        }
        catch (RejectedExecutionException e){
            assertTrue(true)
        }
    }

    @BeforeEach
    void setUp() {
    }

    @AfterEach
    void tearDown() {
    }
}