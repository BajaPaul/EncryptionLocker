using EncryptionLocker.Common;
using LibraryCoder.MainPageCommon;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class About : Page
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// For this page, use 'AB_' prefix, shorthand for About, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public About()
        {
            InitializeComponent();
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButEmail.Content = mainPage.resourceLoader.GetString("AB_ButProgrammerEmail");
            ButRateApp.Content = mainPage.resourceLoader.GetString("UMP_ButRateApp");
            ButCryptoLink1.Content = mainPage.resourceLoader.GetString("AB_ButCryptoLink1");
            ButCryptoLink2.Content = mainPage.resourceLoader.GetString("AB_ButCryptoLink2");
            ButCryptoLink3.Content = mainPage.resourceLoader.GetString("AB_ButCryptoLink3");
            ButCryptoLink4.Content = mainPage.resourceLoader.GetString("AB_ButCryptoLink4");
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Set buttons on page to same size.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
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
            // Set size of buttons on About page to same size.
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButEmail,
                ButRateApp,
                ButCryptoLink1,
                ButCryptoLink2,
                ButCryptoLink3,
                ButCryptoLink4
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("AB_TblkPageTitle"));
            LibMPC.OutputMsgSuccess(TblkPayment, mainPage.resourceLoader.GetString("AB_TblkPayment"));
            LibMPC.OutputMsgNormal(TblkProgrammer, LibMPC.JoinListString(Translate.TRS_AB_List_TblkProgrammer_Text, EnumStringSeparator.OneSpace));    // Do not assemble string until needed to save memory.
            LibMPC.OutputMsgNormal(TblkLink, mainPage.resourceLoader.GetString("AB_TblkLink"));
            LibMPC.OutputMsgBright(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_AB_List_TblkPageMsg_Text, EnumStringSeparator.TwoNewlines));   // Do not assemble string until needed to save memory.
            LibMPC.ButtonEmailXboxDisable(ButEmail);
            //Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            ButRateApp.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Invoked when user clicks a button requesting link to more information.
        /// </summary>
        /// <param name="sender">A button with a Tag that contains hyperlink string.</param>
        /// <param name="e"></param>
        private async void ButURL_Click(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            await LibMPC.ButtonHyperlinkLaunchAsync((Button)sender);
        }

        /// <summary>
        /// Invoked when user clicks ButRateApp. MS Store popup box will lock out all access to App
        /// so do not need to hide other buttons on page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButRateApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await mainPage.RateAppInW10Store();
        }

    }
}
