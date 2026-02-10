using System.ComponentModel;

namespace MuhasibPro.ViewModels.Insrastructure.Common;

public interface INotifyExpressionChanged : INotifyPropertyChanged
{
    void NotifyPropertyChanged(string propertyName);
}
