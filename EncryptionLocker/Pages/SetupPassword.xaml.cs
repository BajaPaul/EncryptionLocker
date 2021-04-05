using EncryptionLocker.Common;
using LibraryCoder.AesEncryption;
using LibraryCoder.MainPageCommon;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class SetupPassword : Page
    {
        /// <summary>
        /// Pointer to MainPage is needed to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// If true, skip one pass of code in PwBoxFR02Pw1_PasswordChanged().  Toggles to true if last password entered by user was invalid.
        /// </summary>
        private bool boolPasswordError1 = false;

        /// <summary>
        /// If true, skip one pass of code in PwBoxFR02Pw2_PasswordChanged().  Toggles to true if last password entered by user was invalid.
        /// </summary>
        private bool boolPasswordError2 = false;

        /// <summary>
        /// False until passwords match in both PasswordBoxes.
        /// </summary>
        private bool boolPasswordsMatch = false;

        /// <summary>
        /// For this page, use 'SP_' prefix, shorthand for SetupPassword, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public SetupPassword()
        {
            InitializeComponent();
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButContinue.Content = mainPage.resourceLoader.GetString("UMP_ButContinue");
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Try to setup User's password. If success, then initial variables as
        /// needed and navigate to page HomePage. Otherwise ask for new password.
        /// </summary>
        /// <returns></returns>
        private void PasswordTryCreate()
        {
            try
            {
                if (PwBoxPw2.Password.Length >= mainPage.uintPasswordLengthMinimum)   // Double check that password meets minimum length requirement.
                {
                    mainPage.cryptographicKeyAppPassword = LibAES.CryptographicKeyPassword(PwBoxPw2.Password);
                    if (mainPage.cryptographicKeyAppPassword != null)
                    {
                        string stringLockerPathGet = mainPage.DataStoreLockerPath(true, mainPage.stringLockerPath);
                        if (stringLockerPathGet != null)
                        {
                            mainPage.ShowPageHomePage();     // Everything as expected so navigate to page HomePage.
                            return;     // Successful completion so return.
                        }
                        else
                            ErrorOccurred(mainPage.resourceLoader.GetString("SP_Error_Password_Setup"));    // Could not save locker setup data.  Please try different password.
                    }
                    else
                        ErrorOccurred(mainPage.resourceLoader.GetString("UMP_Error_Password_Hash"));        // Could not hash password.  Try different password.
                }
                else
                    ErrorOccurred(mainPage.resourceLoader.GetString("SP_Error_Password_Length"));   // Password must have at least four characters.  Please try different password.
            }
            catch (Exception ex)
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("SetupPassword.PasswordTryCreate()", ex.GetType()));
            }
        }

        /// <summary>
        /// Display error messages to user and set other UI items as required..
        /// </summary>
        /// <param name="stringErrorMessage">Error message to display in TblkSPResult.</param>
        private void ErrorOccurred(string stringErrorMessage)
        {
            boolPasswordError1 = true;              // Skip one pass on password entry error.
            PwBoxPw1.Password = string.Empty;       // Change causes event to fire.
            boolPasswordError2 = true;              // Skip one pass on password entry error.
            PwBoxPw2.Password = string.Empty;       // Change causes event to fire.
            LibMPC.OutputMsgError(TblkResult, stringErrorMessage);
            // HideContinue button until password and subfolder successfully created.
            LibMPC.ButtonVisibility(ButContinue, false);
            PwBoxPw1.Focus(FocusState.Programmatic);
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// On page load set focus to SP_PwBoxPw1.
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
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButSettings, false);
            LibMPC.ButtonVisibility(ButContinue, false);    // Hide button until needed.
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("SP_TblkPageTitle"));
            PwBoxPw1.PlaceholderText = mainPage.resourceLoader.GetString("SP_PwBoxPw1_PlaceholderText");
            PwBoxPw2.PlaceholderText = mainPage.resourceLoader.GetString("SP_PwBoxPw2_PlaceholderText");
            if (mainPage.boolVerbose)   // Show long message.
                LibMPC.OutputMsgBright(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_SP_List_TblkPageMsg_Text_Long, EnumStringSeparator.TwoSpaces));    // Do not assemble string until needed to save memory.
            else                        // Show short message.
                LibMPC.OutputMsgBright(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_SP_List_TblkPageMsg_Text_Short, EnumStringSeparator.TwoSpaces));   // Do not assemble string until needed to save memory.
            LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("SP_PwBoxPw1_PlaceholderText"));      // Enter password
            //Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            PwBoxPw1.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Invoked when user presses any key while in PasswordBox but skips all key presses until 'Enter' key is pressed.
        /// If 'Enter' key is pressed, then move focus to PasswordBox 'SP_PwBoxPw2' for password entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwBoxPw1_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            if (e.Key == Windows.System.VirtualKey.Enter)   // Check if 'Enter' key was pressed.  Skip everything else.
            {
                e.Handled = true;   // Acknowledge that event has been handled.
                PwBoxPw2.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        /// Get user entered password and verify if matches other PasswordBox.
        /// Only basic check is needed since any invalid passwords will be rejected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwBoxPw1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (!boolPasswordError1)
            {
                if (PwBoxPw1.Password.Length >= mainPage.uintPasswordLengthMinimum && PwBoxPw1.Password.Equals(PwBoxPw2.Password))  // Check if passwords valid and equal.
                {
                    boolPasswordsMatch = true;
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.resourceLoader.GetString("SP_Success_Password_Match"));
                    LibMPC.ButtonVisibility(ButContinue, true);
                }
                else
                {
                    boolPasswordsMatch = false;
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("SP_Error_Password_Match"));
                    LibMPC.ButtonVisibility(ButContinue, false);
                }
            }
            boolPasswordError1 = false;
        }

        /// <summary>
        /// Invoked when user presses any key while in PasswordBox but skips all key presses until Enter key is pressed.
        /// If Enter key is pressed, then check if ButContinue is enabled. If so, then run ButContinue_Click().
        /// Otherwise, set focus back to PasswordBox SP_PwBoxPw1.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwBoxPw2_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            if (e.Key == Windows.System.VirtualKey.Enter)   // Check if 'Enter' key was pressed.  Skip everything else.
            {
                e.Handled = true;   // Acknowledge that event has been handled.
                if (boolPasswordsMatch)
                {
                    LibMPC.ButtonVisibility(ButContinue, true);
                    ButContinue.Focus(FocusState.Programmatic);
                }
                else
                {
                    LibMPC.ButtonVisibility(ButContinue, false);
                    PwBoxPw1.Focus(FocusState.Programmatic);
                }
            }
        }

        /// <summary>
        /// Get user entered password and verify if matches other PasswordBox.
        /// Only basic check is needed since any invalid passwords will be rejected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwBoxPw2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (!boolPasswordError2)
            {
                if (PwBoxPw2.Password.Length >= mainPage.uintPasswordLengthMinimum && PwBoxPw2.Password.Equals(PwBoxPw1.Password))       // Check if passwords valid and equal.
                {
                    boolPasswordsMatch = true;
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.resourceLoader.GetString("SP_Success_Password_Match"));
                    LibMPC.ButtonVisibility(ButContinue, true);
                }
                else
                {
                    boolPasswordsMatch = false;
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("SP_Error_Password_Match"));
                    LibMPC.ButtonVisibility(ButContinue, false);
                }
            }
            boolPasswordError2 = false;
        }

        /// <summary>
        /// Create password.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButContinue_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            PasswordTryCreate();
        }

    }
}
 