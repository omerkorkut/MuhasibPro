namespace MuhasibPro.Contracts.UIService;

public interface IThemeSelectorService
{
    ElementTheme Theme
    {
        get;
    }
    event EventHandler<ElementTheme> ThemeChanged;
    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    Task SetRequestedThemeAsync();
   
}
