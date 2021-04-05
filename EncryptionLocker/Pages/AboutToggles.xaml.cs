using EncryptionLocker.Common;
using LibraryCoder.MainPageCommon;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class AboutToggles : Page
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// For this page, use 'AT_' prefix, shorthand for AboutToggles, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public AboutToggles()
        {
            InitializeComponent();
        }

        /*** Page Events *******************************************************************************************************/

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Hide XAML layout rectangles by setting their color to RelativePanel Background color;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, true);
            LibMPC.ButtonVisibility(mainPage.mainPageButSettings, false);
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("AT_TblkPageTitle"));
            LibMPC.OutputMsgBright(TblkToggleMsg, LibMPC.JoinListString(Translate.TRS_AT_List_TblkToggleMsg_Text, EnumStringSeparator.TwoNewlines));   // Do not assemble string until needed to save memory.
            //Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
        }
    }
}