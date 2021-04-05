using EncryptionLocker.Common;
using LibraryCoder.AesEncryption;
using LibraryCoder.MainPageCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class EnterPassword : Page
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// App needs to be reset if true, false otherwise.
        /// </summary>
        private bool boolAppResetRequired = false;

        // WARNING: Do not change value without editing value in resource EP_Error_Password_NotValid and EP_Error_LockdownActivated for all languages.
        /// <summary>
        /// Maximum number of password attempts allowed before locker reset. Value is 5.
        /// </summary>
        private readonly int intPasswordAttemptsMax = 5;

        /// <summary>
        /// Current password attempt.
        /// </summary>
        private int intPasswordAttempts = 0;

        /// <summary>
        /// For this page, use 'EP_' prefix, shorthand for EnterPassword, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public EnterPassword()
        {
            InitializeComponent();
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButContinue.Content = mainPage.resourceLoader.GetString("UMP_ButContinue");
            ButLockerReset.Content = mainPage.resourceLoader.GetString("UMP_ButLockerReset");
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// This method checks if entered password is valid. Otherwise, wrong password was entered so request user to enter another one.
        /// If password is valid then navigate to page HomePage.
        /// </summary>
        private void PasswordVerify()
        {
            mainPage.cryptographicKeyAppPassword = LibAES.CryptographicKeyPassword(PwBoxPw.Password);
            if (mainPage.cryptographicKeyAppPassword != null)
            {
                string stringStoreValue = mainPage.DataStoreLockerPath(false, null);
                if (stringStoreValue != null)
                {
                    if (stringStoreValue.Equals(mainPage.stringLockerPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Password is valid if decrypted path using entered password is same as locker folder path.
                        // Debug.WriteLine($"PasswordVerify(): Password valid since {stringStoreValue}={mainPage.stringLockerPath}");
                        mainPage.ShowPageHomePage();    // Everything as expected so navigate to page HomePage.
                        return;     // Success. Correct password was entered.
                    }
                    else
                    {
                        // To test this error path, negate value in 'if' above and then run App. Then enter correct password.
                        boolAppResetRequired = true;
                        PwBoxPw.Visibility = Visibility.Collapsed;
                        LibMPC.OutputMsgError(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_EP_Error_PasswordVerify, EnumStringSeparator.TwoSpaces));    // Do not assemble string until needed to save memory.
                        TblkResult.Text = string.Empty;
                        TblkLockerResetMsg.Visibility = Visibility.Collapsed;
                        LibMPC.ButtonVisibility(ButLockerReset, false);     // Hide button so User is forced to click Continue to start Locker Reset.
                    }
                }
                else
                    PasswordWrong();
            }
            else
                ErrorOccurred(mainPage.resourceLoader.GetString("UMP_Error_Password_Hash"));   // Could not hash password.  Try different password.
        }

        /// <summary>
        /// Display wrong password entered message. Initiate locker reset after too many attempts.
        /// Intent is make difficult to keep attemping to guessing password without jumping through a bunch of hoops.
        /// Windows 10 Apps are not friendly to time based delays. Locker reset is best option since will require whole setup procedure 
        /// to be completed each time to test a different password.
        /// </summary>
        private void PasswordWrong()
        {
            intPasswordAttempts++;
            // Debug.WriteLine($"PasswordWrong(): intPasswordAttempts={intPasswordAttempts}");
            // Allow User intPasswordAttemptsMax attempts to enter correct password before locker reset initiated.
            if (intPasswordAttempts < intPasswordAttemptsMax)
                ErrorOccurred(mainPage.resourceLoader.GetString("EP_Error_Password_NotValid"));    // Password not valid.  Please try again.  Locker reset will be initiated after five attempts.
            else
            {
                // Debug.WriteLine($"PasswordWrong(): Locker reset required since incorrect password entered five times.");
                boolAppResetRequired = true;
                PwBoxPw.Visibility = Visibility.Collapsed;
                LibMPC.OutputMsgError(TblkPageMsg, mainPage.resourceLoader.GetString("EP_Error_LockdownActivated"));   // Locker reset required since incorrect password entered five times.
                TblkResult.Text = string.Empty;
                TblkLockerResetMsg.Visibility = Visibility.Collapsed;
                ButLockerReset.Visibility = Visibility.Collapsed;     // Hide this so User is forced to click Continue to start Locker Reset.
            }
        }

        /// <summary>
        /// Invalid password was entered so reset values and App display.  Also skip one pass of code in 
        /// PwBoxEPPw_PasswordChanged() event so user can see error message that last password was not valid.
        /// </summary>
        /// <param name="stringErrorMessage">Error message to show user after display is reset.</param>
        private void ErrorOccurred(string stringErrorMessage)
        {
            PwBoxPw.Password = string.Empty;
            LibMPC.OutputMsgError(TblkResult, stringErrorMessage);
            PwBoxPw.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Get locker folder from FutureAccessList and set some pages values.
        /// </summary>
        /// <returns></returns>
        private async Task GetLockerFolder()
        {
            try
            {
                // Uncomment following line to delete FutureAccessList to test the fail code below.
                // StorageApplicationPermissions.FutureAccessList.Clear();

                // Before navigating to this page it was confirmed that file 'mainPage.stringFilenameFirstRunCompleted' does exist.
                // Therefore user has set up a folder to use for his/her locker and it has been saved to FutureAccessList.
                // Now retrieve folder from FutureAccessList so this app will have Read/Write access to all contents 'inside'User's 
                // selected folder.  Note!!! If User manually renames or moves file outside of this App this will still return the file!
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(mainPage.stringTokenLocker))
                {
                    mainPage.storageFolderLocker = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(mainPage.stringTokenLocker);
                    if (mainPage.storageFolderLocker != null)
                    {
                        mainPage.stringLockerPath = mainPage.storageFolderLocker.Path;  // Initialize.
                        List<string> list_stringMessagePage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("EP_Success_PageLoad_Msg01"),  // Found locker folder at
                            mainPage.stringLockerPath,
                            mainPage.resourceLoader.GetString("EP_Success_PageLoad_Msg02")   // Application has read and write access to folder.
                        };
                        LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_stringMessagePage, EnumStringSeparator.OneNewline));
                        PwBoxPw.Focus(FocusState.Programmatic);
                        return;     // Success so return.
                    }
                }
                // Should never get here but if do, then Continue button click will force a Locker Reset. Never exit App as per MS guidelines.
                // Uncomment line mentioned above to test this failure path!
                boolAppResetRequired = true;
                LibMPC.OutputMsgError(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_EP_Error_PageLoad_Msg01, EnumStringSeparator.TwoSpaces));  // Do not assemble string until needed to save memory.
                PwBoxPw.Visibility = Visibility.Collapsed;
                TblkLockerResetMsg.Visibility = Visibility.Collapsed;
                ButLockerReset.Visibility = Visibility.Collapsed;     // Hide this so User is forced to click Continue to force a Locker Reset.
                // ButEPContinue click will now run mainPage.ResetLockerAsync() to reset locker.  Never exit App as per MS guidelines.
            }
            catch (System.IO.FileNotFoundException)     // Handle exception.
            {
                // Exception will be thrown if Locker folder no longer exist. To test this failure path, delete locker or save locker on USB stick. Then exit, remove stick, and run again.
                boolAppResetRequired = true;
                LibMPC.OutputMsgError(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_EP_Error_PageLoad_Msg02, EnumStringSeparator.TwoSpaces));  // Do not assemble string until needed to save memory.
                PwBoxPw.Visibility = Visibility.Collapsed;
                TblkLockerResetMsg.Visibility = Visibility.Collapsed;
                ButLockerReset.Visibility = Visibility.Collapsed;       // Hide this so User is forced to click Continue to start Locker Reset.
                // ButEPContinue click will now run mainPage.ResetLockerAsync() to reset locker. Never just exit App as per MS guidelines.
            }
            catch (Exception ex)    // Catch any other exception.
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("EnterPassword.Page_Loaded()", ex.GetType()));
            }
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Try to retrieve locker folder from FutureAccessList and set focus to PwBoxEPPw if success. Otherwise show error message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Hide XAML layout rectangles by setting to same color as RelativePanel Background;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButSettings, false);
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButContinue,
                ButLockerReset
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            ButLockerReset.Foreground = LibMPC.colorError;
            TblkResult.Text = string.Empty;     // Clear placeholder text.
            PwBoxPw.PlaceholderText = mainPage.resourceLoader.GetString("EP_PwBox_PlaceholderText");        // Password
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("EP_TblkPageTitle"));  // Application password entry
            LibMPC.OutputMsgBright(TblkPageMsg, mainPage.resourceLoader.GetString("EP_TblkPageMsg"));       // Enter password to unlock and open locker folder.
            LibMPC.OutputMsgBright(TblkLockerResetMsg, mainPage.GetLockerResetMsg());                       // Get Locker Reset message.
            // Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            await GetLockerFolder();
        }

        /// <summary>
        /// Invoked when user presses any key while in PasswordBox but skips all key presses until 'Enter' key is pressed.
        /// If 'Enter' key is pressed, then get input from PasswordBox and then proceed as if 'Continue' button has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwBoxPw_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            if (e.Key == Windows.System.VirtualKey.Enter)   // Check if 'Enter' key was pressed.  Skip everything else.
            {
                e.Handled = true;   // Acknowledge that event has been handled.
                PasswordVerify();
            }
        }

        /// <summary>
        /// Continue button is disabled until valid length password is entered into password box.
        /// This Button is also used to reset Locker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButContinue_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (boolAppResetRequired)
            {
                // Error occurred! App requires a Locker Reset which returns to page SetupFolder().
                mainPage.AppReset(EnumResetApp.ResetLockerFolder);
            }
            else
                PasswordVerify();    // All good so get User's password.
        }

        /// <summary>
        /// Reset Locker.
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
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Reset_Aborted"));   // Reset aborted.
        }

    }
}
