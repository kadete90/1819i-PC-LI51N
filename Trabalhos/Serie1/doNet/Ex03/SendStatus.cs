namespace Ex03
{
    public interface SendStatus
    {
        bool isSent();
        bool tryCancel();
        bool await(int timeout); // throws InterruptedException;
    }
}
