namespace OATControl.ViewModels
{
    public interface IPolarAlignDialog
    {
        void SetStatus(string statusType, string message);
        void Show();
        void Close();
    }
}
