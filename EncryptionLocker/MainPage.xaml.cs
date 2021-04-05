using EncryptionLocker.Common;
using EncryptionLocker.Pages;
using LibraryCoder.AesEncryption;
using LibraryCoder.MainPageCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// TODO: Add more translations?

// Following Enum is generally unique for each App so place here.
/// <summary>
/// Enum used to reset App setup values via method AppReset().
/// </summary>
public enum EnumResetApp { DoNothing, ResetApp, ResetPurchaseHistory, ResetRateHistory, ShowDataStoreValues, ResetLockerFolder, ResetFirstRunHistory };

namespace EncryptionLocker
{
    public sealed partial class MainPage : Page
    {
        // TODO: Update version number in next string before publishing application to Microsoft Store.
        /// <summary>
        /// String containing version of application as set in Package.appxmanifest file.
        /// </summary>
        public readonly string stringAppVersion = "2021.4.3";

        /// <summary>
        /// Pointer to MainPage.  Other pages can use this pointer to access public methods and public variables created in MainPage.
        /// </summary>
        public static MainPage mainPagePointer;

        // ALL data store 'ds' strings (keys) used by App are declared here. These are (key, value) pairs. Each key has a matching value.

        /// <summary>
        /// Value is "BoolLockerFolderSelected".
        /// </summary>
        public readonly string ds_BoolLockerFolderSelected = "BoolLockerFolderSelected";

        /// <summary>
        /// Value is "BoolAppPurchased".
        /// </summary>
        public readonly string ds_BoolAppPurchased = "BoolAppPurchased";

        /// <summary>
        /// Value is "BoolAppRated".
        /// </summary>
        public readonly string ds_BoolAppRated = "BoolAppRated";

        /// <summary>
        /// Value is "StringEncryptedLockerPath".
        /// </summary>
        public readonly string ds_StringEncryptedLockerPath = "StringEncryptedLockerPath";

        /// <summary>
        /// Value is "BoolFirstRun".
        /// </summary>
        public readonly string ds_BoolFirstRun = "BoolFirstRun";

        /// <summary>
        /// Value is "BoolCboxSamples".
        /// </summary>
        public readonly string ds_BoolCboxSamples = "BoolCboxSamples";

        /// <summary>
        /// Value is "BoolExitApp".
        /// </summary>
        public readonly string ds_BoolExitApp = "BoolExitApp";

        /// <summary>
        /// Value is "BoolDeleteSecure".
        /// </summary>
        public readonly string ds_BoolDeleteSecure = "BoolDeleteSecure";

        /// <summary>
        /// Value is "BoolVerbose".
        /// </summary>
        public readonly string ds_BoolVerbose = "BoolVerbose";

        /// <summary>
        /// Value is "IntAppRatedCounter".
        /// </summary>
        public readonly string ds_IntAppRatedCounter = "IntAppRatedCounter";

        /// <summary>
        /// Value is "CultureIndex".
        /// </summary>
        public readonly string ds_CultureIndex = "CultureIndex";

        /// <summary>
        /// True if locker folder selected, false otherwise.
        /// </summary>
        public bool boolLockerFolderSelected = false;

        /// <summary>
        /// True if application has been purchased, false otherwise.
        /// </summary>
        public bool boolAppPurchased;

        /// <summary>
        /// True if application has been rated, false otherwise.
        /// </summary>
        public bool boolAppRated;

        /// <summary>
        /// Show User long messages if true, otherwise show short messages. Value is toggled in Settings page. Default is true.
        /// This bool is set to true or false on each App start via method MainPage.SetBoolVerboseValue().
        /// </summary>
        public bool boolVerbose;

        /// <summary>
        /// If true, show message to User on first HomePage load that sample items were created.
        /// </summary>
        public bool boolSamplesCreated = false;

        /// <summary>
        /// True if application purchase check has been competed, false otherwise.
        /// </summary>
        public bool boolPurchaseCheckCompleted = false;

        // https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest
        /// <summary>
        /// Access language 'Resources.resw' file to retrieve language strings for pages.
        /// </summary>
        public ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        /// <summary>
        /// Save current culture string here. This value is used to set margins of RectLayoutLeft and RectLayoutRight depending on culture in use.
        /// This value is set in MainPage.Page_Loaded() event.
        /// </summary>
        public string stringCultureCurrent;

        /// <summary>
        /// Save purchase check output string here for display on page Start if User comes back to page.
        /// </summary>
        public string stringPurchaseCheckOutput;

        /// <summary>
        /// File extension added to plain-text sample files created in SetupFolder.xaml.cs.  Current value is ".txt".
        /// </summary>
        public string stringExtensionTxt = ".txt";

        /// <summary>
        /// Random IV file extension added to and removed from folders and filenames in Locker folder. File extension is ".arc".
        /// </summary>
        public string stringExtensionArchive = ".arc";

        /// <summary>
        /// Random IV file extension added to and removed from files located in folder named stringFoldernameNoArchive. File extension is ".aes".
        /// </summary>
        public string stringExtensionNoArchive = ".aes";

        /// <summary>
        /// Name of NoArchive folder created under Locker folder. NoArchive items are isolated to this subfolder. Folder name is "AES".
        /// </summary>
        public string stringFoldernameNoArchive = "AES";

        /// <summary>
        /// This is output message from FolderCheckAsync(). Generally will not want to display this message unless an error occurrs.
        /// </summary>
        public string stringFolderCheckOutput;

        // More about FutureAccessList: https://docs.microsoft.com/en-us/uwp/api/windows.storage.accesscache.storageapplicationpermissions#Windows_Storage_AccessCache_StorageApplicationPermissions_FutureAccessList

        /// <summary>
        /// FutureAccessList token string used to access App encryption folder selected by user. Current value is "TokenLocker".
        /// </summary>
        public string stringTokenLocker = "TokenLocker";

        /// <summary>
        /// Location of user's locker folder as set by user on App first-run and saved in FutureAccessList. Save set value here for use in other pages as needed.
        /// Sample: C:\Users\xxxx\Documents\Locker
        /// </summary>
        public StorageFolder storageFolderLocker;

        /// <summary>
        /// Path of 'storageFolderLocker'.  This is used often so save value here for use in other pages as needed.
        /// Sample: "C:\Users\xxxx\Documents\Locker"
        /// </summary>
        public string stringLockerPath;

        /// <summary>
        /// Value of User entered plain-text password converted to CryptographicKey for use throughout App.
        /// </summary>
        public CryptographicKey cryptographicKeyAppPassword;


        // Can not change this value to larger number after App is published or will prevent some Users getting to their encrypted data.
        /// <summary>
        /// Save required minimum length of User entered plain-text password here for use as needed. Current value is 4.
        /// </summary>
        public int uintPasswordLengthMinimum = 4;

        /// <summary>
        /// Location App uses to read or write various App settings. Save set value here for use in other pages as needed.
        /// </summary>
        public ApplicationDataContainer applicationDataContainer;

        // Make following MainPage XAML variables public via wrapper so values can be changed using methods in LibMPC and from other locations.

        /// <summary>
        /// Set public value of MainPage XAML ScrollViewerMP.
        /// </summary>
        public ScrollViewer mainPageScrollViewer;

        /// <summary>
        /// Set public value of MainPage XAML ButBack.
        /// </summary>
        public Button mainPageButBack;

        /// <summary>
        /// Set public value of MainPage XAML ButAbout.
        /// </summary>
        public Button mainPageButAbout;

        /// <summary>
        /// Set public value of MainPage XAML ButAbout.
        /// </summary>
        public Button mainPageButSettings;

        /// <summary>
        /// For this page, use 'MP_' prefix, shorthand for MainPage, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            mainPagePointer = this;     // Set pointer to this page at this location since required by various pages, methods, and libraries.
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButBack.Content = resourceLoader.GetString("MP_ButBack");
            ButAbout.Content = resourceLoader.GetString("MP_ButAbout");
            ButSettings.Content = resourceLoader.GetString("MP_ButSettings");
        }

        /*** Public Methods ****************************************************************************************************/

        /// <summary>
        /// Reset App to various states using parameter enumResetApp.
        /// </summary>
        /// <param name="enumResetApp">Enum used to reset App setup values.</param>
        public void AppReset(EnumResetApp enumResetApp)
        {
            switch (enumResetApp)
            {
                case EnumResetApp.DoNothing:                // Do nothing. Most common so exit quick.
                    break;
                case EnumResetApp.ResetApp:                 // Clear all data store settings.
                    applicationDataContainer.Values.Clear();
                    break;
                case EnumResetApp.ResetPurchaseHistory:     // Clear App purchase history forcing a new purchase check.
                    applicationDataContainer.Values.Remove(ds_BoolAppPurchased);
                    boolAppPurchased = false;
                    break;
                case EnumResetApp.ResetRateHistory:         // Clear App rate history.
                    applicationDataContainer.Values.Remove(ds_BoolAppRated);
                    boolAppRated = false;
                    break;
                case EnumResetApp.ShowDataStoreValues:      // Show data store values via Debug.
                    LibMPC.ListDataStoreItems(applicationDataContainer);
                    break;
                case EnumResetApp.ResetLockerFolder:        // Reset Locker folder location.
                    applicationDataContainer.Values.Remove(ds_BoolLockerFolderSelected);
                    boolLockerFolderSelected = false;
                    FrameMP.Navigate(typeof(SetupFolder));
                    FrameMP.BackStack.Clear();              // Clear page navigation history.
                    break;
                case EnumResetApp.ResetFirstRunHistory:     // Clear App first run history so sample CheckBox in page SetupFolder() will be checked.
                    applicationDataContainer.Values.Remove(ds_BoolFirstRun);
                    applicationDataContainer.Values.Remove(ds_BoolCboxSamples);
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"MainPage.AppReset(): enumResetApp={enumResetApp} not found in switch statement.");
            }
        }

        /// <summary>
        /// Open W10 Store App rating dialog box. If dialog box opened, then set settings so User no longer prompted to rate App.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RateAppInW10Store()
        {
            if (await LibMPC.ShowRatingReviewDialogAsync())
            {
                boolAppRated = true;
                applicationDataContainer.Values[ds_BoolAppRated] = true;        // Write setting to data store. 
                applicationDataContainer.Values.Remove(ds_IntAppRatedCounter);  // Remove ds_IntAppRatedCounter since no longer used.
                return true;
            }
            return false;
        }

        /// <summary>
        /// Read and write encrypted Locker path setting ds_StringLockerPath to and from data store. Return path string if successful, null otherwise.
        /// Password is valid if decrypted path using entered password is same as locker folder path.
        /// If parameter boolPathEncrypt is false, then parameter stringPathToLocker is not used and can be set to null.
        /// </summary>
        /// <param name="boolPathEncrypt">If true then encrypt Locker path. Otherwise decrypt locker path and return it.</param>
        /// <param name="stringPathToLocker">Path to current Locker or null if reading path from data store.</param>
        /// <returns></returns>
        public string DataStoreLockerPath(bool boolPathEncrypt, string stringPathToLocker = null)
        {
            if (boolPathEncrypt)    // Encrypt Locker folder path using RandomIV and save to data store.
            {
                // Encrypt Locker folder path using RandomIV.
                string stringStoreValueEncrypted = LibAES.StringEncrypt(stringPathToLocker, cryptographicKeyAppPassword, LibAES.IBufferIVRandom(), EnumModeIV.EmbedIV);
                if (stringStoreValueEncrypted != null)
                {
                    applicationDataContainer.Values[ds_StringEncryptedLockerPath] = stringStoreValueEncrypted;   // Save encrypted path to data store.
                    // Debug.WriteLine($"MainPage.DataStoreLockerPath(): Saved RandomIV encrypted Locker path to data store value stringStoreValueEncrypted: {stringStoreValueEncrypted}");
                }
                return stringStoreValueEncrypted;  // Return useless encrypted string so calling method can check if not null.
            }
            else    // Read and decrypt Locker folder path from data store and then return it.
            {
                if (applicationDataContainer.Values.ContainsKey(ds_StringEncryptedLockerPath))
                {
                    // Decrypt Locker folder path using embeded RandomIV and return.
                    // Debug.WriteLine("MainPage.DataStoreLockerPath(): Decrypted Locker folder path using embeded RandomIV.");
                    return LibAES.StringDecrypt((string)applicationDataContainer.Values[ds_StringEncryptedLockerPath], cryptographicKeyAppPassword, null, EnumModeIV.EmbedIV);     // Return value or null.
                }
            }
            return null;
        }

        /// <summary>
        /// Get Locker Reset output message. This message is used mutiple locations so consolidate here.
        /// </summary>
        /// <returns></returns>
        public string GetLockerResetMsg()
        {
            if (boolVerbose)    // Show long message.
                return LibMPC.JoinListString(Translate.TRS_MP_List_LockerResetMsgLong, EnumStringSeparator.OneNewline);     // Do not assemble string until needed to save memory.
            else                // Show short message.
                return resourceLoader.GetString("UMP_LockerReset_Msg03");
        }

        /// <summary>
        /// Navigate to page SetupFolder.
        /// </summary>
        public void ShowPageSetupFolder()
        {
            FrameMP.Navigate(typeof(SetupFolder));
            FrameMP.BackStack.Clear();      // Clear navigation history.
        }

        /// <summary>
        /// Navigate to page SetupPassword.
        /// </summary>
        public void ShowPageSetupPassword()
        {
            FrameMP.Navigate(typeof(SetupPassword));
            FrameMP.BackStack.Clear();      // Clear navigation history.
        }

        /// <summary>
        /// Navigate to page EnterPassword.
        /// </summary>
        public void ShowPageEnterPassword()
        {
            FrameMP.Navigate(typeof(EnterPassword));
            FrameMP.BackStack.Clear();      // Clear navigation history.
        }

        /// <summary>
        /// Navigate to page HomePage.
        /// </summary>
        public void ShowPageHomePage()
        {
            FrameMP.Navigate(typeof(HomePage));
            FrameMP.BackStack.Clear();      // Clear navigation history after navigating from pages SetupPassword and EnterPassword.
        }

        /// <summary>
        /// Navigate to page AboutToggles.
        /// </summary>
        public void ShowPageAboutToggles()
        {
            FrameMP.Navigate(typeof(AboutToggles));
        }

        /// <summary>
        /// Check if locker folder exists. User can rename or delete Locker folder at any time so need to check if folder exists at start of each process.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FolderCheckLockerAsync()
        {
            try
            {
                // Note: TryGetItemAsync() will not work here since cannot get parent folder of Locker folder.
                // Use following line as alternative and catch FileNotFoundException exception if occurrs.
                await StorageFolder.GetFolderFromPathAsync(stringLockerPath);   // This will throw FileNotFoundException if Locker folder not found.
                // Debug.WriteLine("MainPage.FolderCheckLockerAsync(): Found locker folder so continue.");
                boolLockerFolderSelected = true;
                return true;
            }
            catch (FileNotFoundException)   // This exception is expected if User moves, renames, or deletes locker folder while App is running.
            {
                // Debug.WriteLine("MainPage.FolderCheckLockerAsync(): Did not find locker folder so abort current process.");
                boolLockerFolderSelected = false;
                return false;
                throw;
            }
            catch (Exception ex)
            {
                stringFolderCheckOutput = UnhandledExceptionMessage("MainPage.FolderCheckLockerAsync()", ex.GetType());
                // Debug.WriteLine($"MainPage.FolderCheckLockerAsync(): stringFolderCheckOutput={stringFolderCheckOutput}");
                return false;
                throw;
            }
        }

        /// <summary>
        /// Check if NoArchive folder exists. Return storageFolderNoArchive if found or created successfully, null otherwise.
        /// Output messages are written to public MainPage variable stringFolderCheckOutput if needed.
        /// User can delete NoArchive folder at any time so may need to check if exists.
        /// </summary>
        /// <returns></returns>
        public async Task<StorageFolder> FolderCheckNoArchiveAsync()
        {
            try
            {
                List<string> list_stringFolderCheckOutput = new List<string>();
                StorageFolder storageFolderNoArchive = null;
                // Debug.WriteLine($"MainPage.FolderCheckNoArchiveAsync(): storageFolderLocker.Path={storageFolderLocker.Path}");
                IStorageItem iStorageItem = await storageFolderLocker.TryGetItemAsync(stringFoldernameNoArchive);
                if (iStorageItem != null)   // Item found but don't know if it was folder or file.
                {
                    if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                    {
                        storageFolderNoArchive = (StorageFolder)iStorageItem;     // Found folder.
                    }
                    else
                    {
                        // Item found was not a folder as expected but is using App reserved name.
                        // Delete item to Recycle Bin to be safe and create NoArchive folder.
                        await iStorageItem.DeleteAsync();
                        storageFolderNoArchive = await storageFolderLocker.CreateFolderAsync(stringFoldernameNoArchive);
                    }
                }
                else    // iStorageItem is null, so create NoArchive folder.
                {
                    storageFolderNoArchive = await storageFolderLocker.CreateFolderAsync(stringFoldernameNoArchive);
                }
                if (storageFolderNoArchive != null)
                    list_stringFolderCheckOutput.Add(resourceLoader.GetString("MP_Success_FolderCheck_NoArchive"));     // Found or created folder
                else
                    list_stringFolderCheckOutput.Add(resourceLoader.GetString("MP_Error_FolderCheck_NoArchive"));       // Could not find or create folder
                list_stringFolderCheckOutput.Add(storageFolderNoArchive.Name);
                stringFolderCheckOutput = LibMPC.JoinListString(list_stringFolderCheckOutput, EnumStringSeparator.OneSpace);
                // Debug.WriteLine($"MainPage.FolderCheckNoArchiveAsync(): stringFolderCheckOutput={stringFolderCheckOutput}");
                // Debug.WriteLine($"MainPage.FolderCheckNoArchiveAsync(): storageFolderNoArchive.Path={storageFolderNoArchive.Path}");
                // throw new ArgumentException("Throw exception to test exception methods. Comment this out when satisfied all is working.");
                return storageFolderNoArchive;    // Is null on error.
            }
            catch (Exception ex)
            {
                stringFolderCheckOutput = UnhandledExceptionMessage("MainPage.FolderCheckNoArchiveAsync()", ex.GetType());
                return null;
                throw;
            }
        }

        /// <summary>
        /// Format and return uniform unhandled exception message.
        /// </summary>
        /// <param name="stringMethodName">Name of method exception occurred in. Format as 'PageName: MethodName'.</param>
        /// <param name="objectGetType">Exception value returned by GetType().</param>
        /// <returns></returns>
        public string UnhandledExceptionMessage(string stringMethodName, object objectGetType)
        {
            List<string> list_UnhandledExceptionMsg = new List<string>()
            {
                resourceLoader.GetString("MP_UnhandledException_Msg"),
                stringMethodName,
                $"{Environment.NewLine}{resourceLoader.GetString("MP_UnhandledException_Type")}",     // Place string on newline.
                objectGetType.ToString()
            };
            return LibMPC.JoinListString(list_UnhandledExceptionMsg, EnumStringSeparator.OneSpace);
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Determine which page to navigate too and then go there.
        /// </summary>
        private void NavigateFirstPage()
        {
            if (boolLockerFolderSelected)
            {
                // Debug.WriteLine($"MainPage.NavigateFirstPage(): boolLockerFolderSelected={boolLockerFolderSelected} so navigate to ShowPageEnterPassword()");
                ShowPageEnterPassword();
            }
            else
            {
                // Debug.WriteLine($"MainPage.NavigateFirstPage(): boolLockerFolderSelected={boolLockerFolderSelected} so navigate to ShowPageSetupFolder()");
                ShowPageSetupFolder();
            }
        }

        /// <summary>
        /// Back-a-page navigation event handler. Invoked when software or hardware back button is selected, 
        /// or Windows key + Backspace is entered, or say, "Hey Cortana, go back".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackRequestedPage(object sender, BackRequestedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            // If event has not already been handled then navigate back to previous page.
            // Next if statement required to prevent App from ending abruptly on a back event.
            if (FrameMP.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                PageGoBack();
            }
        }

        /// <summary>
        /// Navigate back a page.
        /// </summary>
        private void PageGoBack()
        {
            if (FrameMP.CanGoBack)
                FrameMP.GoBack();
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Set size of buttons defined in MainPage.xaml to same size.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            TblkAppTitle.Text = resourceLoader.GetString("MP_TblkAppName");
            // Set MainPage public values XAML variables so can be called from library LibMPC.
            mainPageScrollViewer = ScrollViewerMP;
            mainPageButBack = ButBack;
            mainPageButAbout = ButAbout;
            mainPageButSettings = ButSettings;
            // Back-a-page navigation event handler. Invoked when software or hardware back button is selected, 
            // or Windows key + Backspace is entered, or say, "Hey Cortana, go back".
            SystemNavigationManager.GetForCurrentView().BackRequested += BackRequestedPage;
            // Get App data store location.
            // https://msdn.microsoft.com/windows/uwp/app-settings/store-and-retrieve-app-data#local-app-data
            applicationDataContainer = ApplicationData.Current.LocalSettings;
            LibMPC.CustomizeAppTitleBar();
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButBack,
                ButAbout,
                ButSettings
            };
            LibMPC.SizePageButtons(listButtonsThisPage);

            // TODO: set next line to EnumResetApp.DoNothing before store publish.
            AppReset(EnumResetApp.DoNothing);   // Reset App to various states.

            // TODO: Comment out next 5 code lines before App publish.
            // StorageFolder storageFolderApp = ApplicationData.Current.LocalFolder;
            // Debug.WriteLine($"MainPage().Page_Loaded(): storageFolderApp.Path={storageFolderApp.Path}");
            // AppReset(EnumResetApp.ShowDataStoreValues);   // Show data store values.
            // https://stackoverflow.com/questions/12799619/how-to-get-actual-language-in-a-winrt-app/15135121#15135121
            // Debug.WriteLine($"MainPage().Page_Loaded(): Device culture is {LibMPC.GetCulture(true)}");
            // https://msdn.microsoft.com/en-us/library/windows/apps/hh694557.aspx?f=255&MSPPError=-2147217396
            // Debug.WriteLine($"MainPage().Page_Loaded(): Current application culture is {LibMPC.GetCulture(false)}");

            // Get application culture. Some translations may need this value to tweak page output display.
            stringCultureCurrent = LibMPC.GetCulture(false);
            // Get data store values for next four items and set to true or false.
            boolLockerFolderSelected = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolLockerFolderSelected);
            boolAppPurchased = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolAppPurchased);
            boolAppRated = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolAppRated);
            boolVerbose = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolVerbose);
            // AppReset(EnumResetApp.ShowDataStoreValues);     // TODO: Comment out this line before store publish. Show data store values.

            LibMPC.ScrollViewerOff(mainPageScrollViewer);  // Turn ScrollViewerMP off for now.  Individual pages will set it as required.
            NavigateFirstPage();
        }

        /// <summary>
        /// Navigate to page About.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButAbout_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            FrameMP.Navigate(typeof(About));
        }

        /// <summary>
        /// Navigate to page Settings().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButSettings_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            FrameMP.Navigate(typeof(Settings));
        }

        /// <summary>
        /// Navigate back to previous page when back button in title bar is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButBack_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            PageGoBack();
        }

    }
}
