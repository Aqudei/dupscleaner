using Caliburn.Micro;

namespace DupFileCleaner.ViewModels
{
    sealed class ShellViewModel : Conductor<object>.Collection.OneActive
    {
        public ShellViewModel()
        {
            ActivateItem(IoC.Get<FileCleanerViewModel>());
        }
    }
}
