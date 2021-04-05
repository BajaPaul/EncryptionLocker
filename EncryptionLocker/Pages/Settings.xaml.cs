using LibraryCoder.MainPageCommon;
using System;
using System.Collections.Generic;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class Settings : Page
    {
        /// <summary>
        /// Pointer to MainPage is needed to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// Pointer to HomePage is needed to call public methods or variables in HomePage.
        /// </summary>
        private readonly HomePage homePage = HomePage.homePagePointer;

        /// <summary>
        /// Skip cuture setting that occurs when CboxLanguage index is set on page load.
        /// </summary>
        private bool boolPageLoaded = true;

        /// <summary>
        /// For this page, use 'ST_' prefix, shorthand for Settings, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public Settings()
        {
            InitializeComponent();
            // Load language resource values for buttons and ToggleSwitches on page before Page_Loaded() event so items render properly.
            ButAboutToggles.Content = mainPage.resourceLoader.GetString("ST_ButAboutToggles");
            ButLockerReset.Content = mainPage.resourceLoader.GetString("UMP_ButLockerReset");
            ButAppReset.Content = mainPage.resourceLoader.GetString("ST_ButAppReset");
            TogExitApp.Header = mainPage.resourceLoader.GetString("ST_TogExitApp");
            TogExitApp.OnContent = mainPage.resourceLoader.GetString("UMP_On");
            TogExitApp.OffContent = mainPage.resourceLoader.GetString("UMP_Off");
            TogDeleteSecure.Header = mainPage.resourceLoader.GetString("ST_TogDeleteSecure");
            TogDeleteSecure.OnContent = mainPage.resourceLoader.GetString("UMP_On");
            TogDeleteSecure.OffContent = mainPage.resourceLoader.GetString("UMP_Off");
            TogVerbose.Header = mainPage.resourceLoader.GetString("ST_TogVerbose");
            TogVerbose.OnContent = mainPage.resourceLoader.GetString("UMP_On");
            TogVerbose.OffContent = mainPage.resourceLoader.GetString("UMP_Off");
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Set state of toggles boolExitApp, boolDeleteSecure, and boolVerbose.
        /// </summary>
        private void SetToggles()
        {
            // Debug.WriteLine($"Settings.SetToggles(): homePage.boolExitApp={homePage.boolExitApp}, homePage.boolDeleteSecure={homePage.boolDeleteSecure}, mainPage.boolVerbose={mainPage.boolVerbose}");
            if (homePage.boolExitApp)
                TogExitApp.IsOn = true;
            else
                TogExitApp.IsOn = false;
            if (homePage.boolDeleteSecure)
                TogDeleteSecure.IsOn = true;
            else
                TogDeleteSecure.IsOn = false;
            if (mainPage.boolVerbose)
                TogVerbose.IsOn = true;
            else
                TogVerbose.IsOn = false;
        }

        /// <summary>
        /// Read or create data store value ds_CultureIndex.
        /// </summary>
        private int DataStoreCultureIndex()
        {
            int cultureIndex = 0;   // 0 is default.
            if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_CultureIndex))
            {
                object objectCultureIndex = mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex];
                cultureIndex = (int)objectCultureIndex;
            }
            else    // Did not find key so set value to default.
            {
                mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 0;         // Write default culture setting to data store.
            }
            return cultureIndex;
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Set focus to ST_HButAboutToggles when page loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Hide XAML layout rectangles by setting to same color as RelativePanel Background;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, true);
            LibMPC.ButtonVisibility(mainPage.mainPageButSettings, false);
            ButAppReset.Foreground = LibMPC.colorError; List<ToggleSwitch> listToggleSwitchesThisPage = new List<ToggleSwitch>()
            {
                TogExitApp,
                TogDeleteSecure,
                TogVerbose
            };
            LibMPC.SizePageToggleSwitches(listToggleSwitchesThisPage);
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButAboutToggles,
                ButLockerReset,
                ButAppReset
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            CboxLanguage.MinWidth = ButAboutToggles.ActualWidth;    // Set CboxLanguage.MinWidth to value of other buttons on page.
            ButLockerReset.Foreground = LibMPC.colorError;
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("ST_TblkPageTitle"));
            CboxLanguage.PlaceholderText = mainPage.resourceLoader.GetString("ST_CboxLanguage_PlaceholderText");
            // XAML margin setting is 16,2,16,2. Margin Setting Order: left, top, right, bottom.
            // Margin for ToggleSwitches needs to be adjusted for some cultures to keep them centered.
            // TODO: Will need to add new item here if a new translation is added!
            switch (mainPage.stringCultureCurrent)
            {
                case "en-US":
                    TogExitApp.Margin = new Thickness(29, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(29, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(29, 2, 2, 2);
                    break;
                case "en":
                    TogExitApp.Margin = new Thickness(29, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(29, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(29, 2, 2, 2);
                    break;
                case "es":
                    TogExitApp.Margin = new Thickness(17, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(17, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(17, 2, 2, 2);
                    break;
                case "hi":
                    TogExitApp.Margin = new Thickness(17, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(17, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(17, 2, 2, 2);
                    break;
                case "fr":
                    TogExitApp.Margin = new Thickness(17, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(17, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(17, 2, 2, 2);
                    break;
                case "zh-Hans":
                    TogExitApp.Margin = new Thickness(44, 2, 2, 2);
                    TogDeleteSecure.Margin = new Thickness(44, 2, 2, 2);
                    TogVerbose.Margin = new Thickness(44, 2, 2, 2);
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"Settings.Page_Loaded(): mainPage.stringCultureCurrent={mainPage.stringCultureCurrent} not found in switch statement.");
            }

            // TODO: Will need to add new item here if a new translation is added!
            CboxItem_LanguageDevice.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageDevice");
            CboxItem_LanguageEN.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageEN");
            CboxItem_LanguageES.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageES");
            CboxItem_LanguageHI.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageHI");
            CboxItem_LanguageFR.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageFR");
            CboxItem_LanguageZH.Content = mainPage.resourceLoader.GetString("ST_CboxItem_LanguageZH");
            CboxLanguage.SelectedIndex = DataStoreCultureIndex();   // Get saved index value from data store.

            LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("ST_ModeToggleMsg"));  // Change application configuration using above toggles.  Click above button for more information about the toggles.
            LibMPC.OutputMsgBright(TblkLockerResetMsg, mainPage.GetLockerResetMsg());    // Get Locker Reset message.
            LibMPC.OutputMsgBright(TblkAppResetMsg, $"{mainPage.resourceLoader.GetString("ST_TblkAppResetMsg")} {mainPage.resourceLoader.GetString("UMP_Reset_Msg")}");
            LibMPC.OutputMsgBright(TblkLanguageMsg, mainPage.resourceLoader.GetString("ST_TblkLanguageMsg"));   // Select application language. Application will close if selection is changed. Selected language will load next application start.
            SetToggles();   // Set this before next line so initial return message will be cleared by next line.  Don't want to see return message on page load.
            TblkResult.Text = string.Empty;
            // Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            ButAboutToggles.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Navigate to 'AboutToggles' page for more information about Secure-Delete and Read-Only modes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButAboutToggles_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            mainPage.ShowPageAboutToggles();
        }

        /// <summary>
        /// Toggle Exit App mode after HomePage 'Encrypt Locker' button picked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogExitApp_Toggled(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.IsOn == true)
                {
                    homePage.boolExitApp = true;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolExitApp] = true;       // Write setting to data store.
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.resourceLoader.GetString("ST_ModeExitAppOn"));     // Exit application mode is on
                }
                else
                {
                    homePage.boolExitApp = false;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolExitApp] = false;      // Write setting to data store.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("ST_ModeExitAppOff"));      // Exit application mode is off
                }
            }
        }

        /// <summary>
        /// Toggle Secure Delete mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogDeleteSecure_Toggled(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.IsOn == true)
                {
                    homePage.boolDeleteSecure = true;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolDeleteSecure] = true;      // Write setting to data store.
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.resourceLoader.GetString("ST_ModeDeleteSecureOn"));    // Secure delete mode is on
                }
                else
                {
                    homePage.boolDeleteSecure = false;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolDeleteSecure] = false;     // Write setting to data store.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("ST_ModeDeleteSecureOff"));     // Secure delete mode is off
                }
            }
        }

        /// <summary>
        /// Toggle Verbose mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogVerbose_Toggled(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.IsOn == true)
                {
                    mainPage.boolVerbose = true;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolVerbose] = true;       // Write setting to data store.
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.resourceLoader.GetString("ST_ModeVerboseOn"));     // Verbose mode is on
                }
                else
                {
                    mainPage.boolVerbose = false;
                    mainPage.applicationDataContainer.Values[mainPage.ds_BoolVerbose] = false;      // Write setting to data store.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("ST_ModeVerboseOff"));      // Verbose mode is off
                }
            }
        }

        /// <summary>
        /// Start Locker reset option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButLockerReset_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Reset locker folder?  Reset will not delete any of your data.
            if (await LibMPC.ShowPopupBoxAsync(mainPage.resourceLoader.GetString("UMP_LockerReset_Title"), mainPage.resourceLoader.GetString("UMP_Reset_Msg"), mainPage.resourceLoader.GetString("UMP_Reset_Yes"), mainPage.resourceLoader.GetString("UMP_Reset_No")))
                mainPage.AppReset(EnumResetApp.ResetLockerFolder);
            else
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Reset_Aborted"));               // Reset aborted.
        }

        // TODO: Need to edit this method if new culture (language) is added.
        /// <summary>
        /// Select culture (language) option to use for App.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CboxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (!boolPageLoaded)    // Skip any changes when page loads.
            {
                if (CboxLanguage.SelectedIndex > -1)    // Something was selected.
                {
                    int itemSelected = CboxLanguage.SelectedIndex;
                    switch (itemSelected)
                    {
                        case 1:
                            // English: Use LanguageEN setting override.
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 1;
                            ApplicationLanguages.PrimaryLanguageOverride = "en";
                            break;
                        case 2:
                            // Spanish: Use LanguageES setting override.
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 2;
                            ApplicationLanguages.PrimaryLanguageOverride = "es";
                            break;
                        case 3:
                            // Hindi: Use LanguageHI setting override.
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 3;
                            ApplicationLanguages.PrimaryLanguageOverride = "hi";
                            break;
                        case 4:
                            // French: Use LanguageFR setting override.
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 4;
                            ApplicationLanguages.PrimaryLanguageOverride = "fr";
                            break;
                        case 5:
                            // Chinese: Use LanguageZH setting override.
                            // Chinese language does not have a two character override such as 'zh'.
                            // More at:  https://stackoverflow.com/questions/4892372/language-codes-for-simplified-chinese-and-traditional-chinese
                            // Debug.WriteLine($"CboxLanguage_SelectionChanged():  itemSelected={itemSelected} so set to zh-Hans");
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 5;
                            ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans";   // Chinese (Simplified).
                            break;
                        default:
                            // Default is case 0: Use LanguageDevice setting.
                            mainPage.applicationDataContainer.Values[mainPage.ds_CultureIndex] = 0;         // Write culture index setting to data store.
                            ApplicationLanguages.PrimaryLanguageOverride = string.Empty;    // String.Empty clears any culture overrides and App will default to Device settings.
                            // More at: https://stackoverflow.com/questions/35487795/how-to-reset-the-primarylanguageoverride
                            // Debug.WriteLine($"CboxLanguage_SelectionChanged():  Default itemSelected={itemSelected} so language set to device default.");
                            break;
                    }
                    Application.Current.Exit();     // Exit application since selection changed. All pages need to be refreshed for new language.
                }
            }
            boolPageLoaded = false;
        }

        /// <summary>
        /// Reset App to new condition by clearing all data store settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButAppReset_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Reset application settings?  Reset will not delete any of your data.
            if (await LibMPC.ShowPopupBoxAsync(mainPage.resourceLoader.GetString("ST_ApplicationReset_Title"), mainPage.resourceLoader.GetString("UMP_Reset_Msg"), mainPage.resourceLoader.GetString("UMP_Reset_Yes"), mainPage.resourceLoader.GetString("UMP_Reset_No")))
            {
                mainPage.AppReset(EnumResetApp.ResetApp);
                Application.Current.Exit();     // Exit App to complete reset.
            }
            else
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Reset_Aborted"));  // Reset aborted.
        }

    }
}
