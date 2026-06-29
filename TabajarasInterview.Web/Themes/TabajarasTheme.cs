using MudBlazor;
using MudBlazor.Utilities;

namespace TabajarasInterview.Web.Themes
{
    public static class TabajarasTheme
    {
        public static MudTheme Theme => new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = new MudColor("#0058AB"),
                Secondary = new MudColor("#12B76A"),
                Tertiary = new MudColor("#FE8D00"),
                Background = new MudColor("#FFFFFF"),
                Surface = new MudColor("#FFFFFF"),
                AppbarBackground = new MudColor("#FFFFFF"),
                AppbarText = new MudColor("#000000"),
                DrawerBackground = new MudColor("#FFFFFF"),
                Success = new MudColor("#12B76A"),
                Info = new MudColor("#0058AB"),
                Warning = new MudColor("#FE8D00"),
                Error = new MudColor("#FF5135"),
                TextPrimary = new MudColor("#000000"),
                TextSecondary = new MudColor("#666666"),
            },
            PaletteDark = new PaletteDark
            {
                Primary = new MudColor("#0058AB"),
                Secondary = new MudColor("#34D399"),
                Tertiary = new MudColor("#FE8D00"),
                Background = new MudColor("#161927"),
                BackgroundGray = new MudColor("#2C3249"),
                Surface = new MudColor("#252A40"),
                AppbarBackground = new MudColor("#161927"),
                AppbarText = new MudColor("#F1F1F6"),
                DrawerBackground = new MudColor("#252A40"),
                DrawerText = new MudColor("#F1F1F6"),
                DrawerIcon = new MudColor("#A1A8B8"),
                Success = new MudColor("#34D399"),
                Info = new MudColor("#0058AB"),
                Warning = new MudColor("#FE8D00"),
                Error = new MudColor("#FF6B55"),
                TextPrimary = new MudColor("#F1F1F6"),
                TextSecondary = new MudColor("#A1A8B8"),
                TextDisabled = new MudColor("#5A6178"),
                ActionDefault = new MudColor("#F1F1F6"),
                ActionDisabled = new MudColor("#5A6178"),
                LinesDefault = new MudColor("#363D58"),
                Divider = new MudColor("#363D58"),
                DividerLight = new MudColor("#2E3450"),
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Ubuntu", "Helvetica", "Arial", "sans-serif"]
                }
            }
        };
    }
}