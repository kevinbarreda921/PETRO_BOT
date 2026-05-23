namespace PETRO_BOT.Services.Toast
{
    public class ToastService
    {
        public event Action<string, string>? OnShow;

        public void Success(string message)
        {
            OnShow?.Invoke(message, "success");
        }

        public void Error(string message)
        {
            OnShow?.Invoke(message, "error");
        }

        public void Warning(string message)
        {
            OnShow?.Invoke(message, "warning");
        }

        public void Info(string message)
        {
            OnShow?.Invoke(message, "info");
        }
    }
}
