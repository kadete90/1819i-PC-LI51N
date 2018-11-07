namespace Ex1
{

    //Esta implementação reflete a semântica de uma message box contendo no máximo uma mensagem que pode
    //ser consumida múltiplas vezes, contudo não é thread-safe.Implemente em Java ou em C#, sem utilizar locks ,
    //uma versão thread-safe deste sincronizador.

    public class UnsafeMessageBox<T> where T : class
    {
        private class MsgHolder
        {
            internal T msg;
            internal int lives;
        }
        private MsgHolder msgHolder;
        public void Publish(T m, int lvs)
        {
            msgHolder = new MsgHolder { msg = m, lives = lvs };
        }
        public T TryConsume()
        {
            if (msgHolder != null && msgHolder.lives > 0)
            {
                msgHolder.lives -= 1;
                return msgHolder.msg;
            }
            return null;
        }
    }
}
