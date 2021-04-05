using EncryptionLocker.Common;
using LibraryCoder.AesEncryption;
using LibraryCoder.MainPageCommon;
using LibraryCoder.UtilitiesFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace EncryptionLocker.Pages
{
    /// <summary>
    /// Enum to select encrypted FileType compare mode to check if file is encrypted. This is used by StorageFileIsEncrypted().
    /// If Archive, then compare FileType is ".arc".
    /// If NoArchive, then compare FileType is ".aes".
    /// If All, then compare if FileType is ".arc" or ".aes".
    /// If AllButFileTypeToSkip, then compare if FileType is ".arc" or ".aes" but skip FileType ".???".
    /// </summary>
    public enum EnumEncryptedFileTypeCompareMode { Archive, NoArchive, All, AllButFileTypeToSkip };

    public sealed partial class HomePage : Page
    {
        /// <summary>
        /// Pointer to Home.  Other pages can use this pointer to access public methods and public variables created in HomePage.
        /// </summary>
        public static HomePage homePagePointer;

        /// <summary>
        /// Place App in Exit App mode if true. Value can be changed via toggle in Settings page.
        /// If true, App will exit after 'Encrypt Locker' button picked.
        /// </summary>
        public bool boolExitApp;

        /// <summary>
        /// Place App in Secure Delete mode if true. Value can be changed via toggle in Settings page.
        /// </summary>
        public bool boolDeleteSecure;

        /// <summary>
        /// Pointer to MainPage is needed to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        // WARNING: CANNOT CHANGE THIS VALUE OR EXISTING USERS WILL NOT BE ABLE TO DECRYPT ENCRYPTED FILES SUCCESSFULLY.
        // ALTERNATIVE IS TO FOREVER PROVIDE WORKAROUND CODE TO MIGRATE EXISTING FILES TO NEW NAME IF THEY ARE DECRYPTED AGAIN.
        /// <summary>
        /// Flagged filename used when encrypting and decrypting a file. If this file exists, then move the other file in the folder, 
        /// which is the original file, back to parent folder. Then delete temp wrapper folder and this flag file. Current value is "EncryptionLocker_FlagFile.txt".
        /// </summary>
        private readonly string stringFilenameFlag = "EncryptionLocker_FlagFile.txt";

        /// <summary>
        /// Timer used by ProgressBarHomeShow() to time encryption and decryption processes.
        /// </summary>
        private readonly Stopwatch stopWatchTimer = new Stopwatch();

        /// <summary>
        /// Elapsed time from 'stopWatchTimer'.  Retrieve this value to get elapsed time.
        /// </summary>
        private TimeSpan timeSpanElapsed = TimeSpan.Zero;

        /// <summary>
        /// Show User ButRateApp button if this number of page loads since last reset.  Current value is 8.
        /// </summary>
        private readonly int intShowButRateApp = 8;

        /// <summary>
        /// Save stringMessage to this global variable so calling methods can retrieve message. 
        /// This is work-a-round since async methods cannot use 'out' variables.
        /// </summary>
        private string stringMessageResult;

        /// <summary>
        /// Used in recursive methods. This value is set before calling recursive method so it knows what type of comparision to make.
        /// This enum is used in StorageFileIsEncrypted().
        /// </summary>
        private string stringEncryptedFileTypeCompareRecursive = null;

        /// <summary>
        /// Used in recursive methods. This value is set before calling recursive method so it knows what type of comparision to make.
        /// This enum is used in StorageFileIsEncrypted().
        /// </summary>
        private static EnumEncryptedFileTypeCompareMode enumEncryptedFileTypeCompareModeRecursive;

        /// <summary>
        /// Used in recursive methods. True if no errors. Changes to false if error processing any files. 
        /// List of filenames causing an error are saved in listFileErrorsRecursive.
        /// </summary>
        private bool boolProcessSuccessRecursive;

        /// <summary>
        /// Used in recursive methods. Number of files found during process. Some files may not have been processed due to error.
        /// </summary>
        public int intFilesFoundRecursive;

        /// <summary>
        /// Used in recursive methods. List of file paths not processed due to error.
        /// </summary>
        private readonly List<string> listFileErrorsRecursive = new List<string>();

        /// <summary>
        /// For this page, use 'HP_' prefix, shorthand for HomePage, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public HomePage()
        {
            InitializeComponent();
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButLockerView.Content = mainPage.resourceLoader.GetString("HP_ButLockerView");
            ButCreateFolderNoArchive.Content = mainPage.resourceLoader.GetString("HP_ButCreateFolderNoArchive");
            ButEncryptLocker.Content = mainPage.resourceLoader.GetString("HP_ButEncryptLocker");
            ButDecryptLocker.Content = mainPage.resourceLoader.GetString("HP_ButDecryptLocker");
            ButEncryptFile.Content = mainPage.resourceLoader.GetString("HP_ButEncryptFile");
            ButDecryptFile.Content = mainPage.resourceLoader.GetString("HP_ButDecryptFile");
            ButEncryptFolder.Content = mainPage.resourceLoader.GetString("HP_ButEncryptFolder");
            ButDecryptFolder.Content = mainPage.resourceLoader.GetString("HP_ButDecryptFolder");
            ButDeleteFile.Content = mainPage.resourceLoader.GetString("HP_ButDeleteFile");
            ButDeleteFolder.Content = mainPage.resourceLoader.GetString("HP_ButDeleteFolder");
            ButPurchaseApp.Content = mainPage.resourceLoader.GetString("HP_ButPurchaseApp");
            ButRateApp.Content = mainPage.resourceLoader.GetString("UMP_ButRateApp");
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Get purchase status of application. Method controls visibility/Enable of PBarStatus, TblkPurchaseApp, and ButPurchaseApp.
        /// </summary>
        private async Task AppPurchaseCheck()
        {
            if (mainPage.boolAppPurchased)
            {
                // App has been purchased so hide following values and return.
                PBarStatus.Visibility = Visibility.Collapsed;
                TblkPurchaseApp.Visibility = Visibility.Collapsed;
                LibMPC.ButtonVisibility(ButPurchaseApp, false);
            }
            else
            {
                if (mainPage.boolPurchaseCheckCompleted)
                {
                    // App has not been purchased but purchase check done so show previous message. This occurs if User returning from another page.
                    PBarStatus.Visibility = Visibility.Collapsed;
                    LibMPC.OutputMsgError(TblkPurchaseApp, mainPage.stringPurchaseCheckOutput);
                    TblkPurchaseApp.Visibility = Visibility.Visible;
                    LibMPC.ButtonVisibility(ButPurchaseApp, true);
                }
                else
                {
                    // App has not been purchased so do purchase check.
                    LibMPC.OutputMsgBright(TblkPurchaseApp, mainPage.resourceLoader.GetString("HP_PurchaseCheckMsg"));  // Checking if application has been purchased.
                    PBarStatus.Foreground = LibMPC.colorError;          // Set color PBarStatus from default.
                    PBarStatus.Visibility = Visibility.Visible;
                    PBarStatus.IsIndeterminate = true;
                    await EnablePageItems(false);
                    mainPage.boolAppPurchased = await LibMPC.AppPurchaseStatusAsync(mainPage.applicationDataContainer, mainPage.ds_BoolAppPurchased);
                    if (mainPage.boolAppPurchased)
                    {
                        // App purchased.
                        LibMPC.OutputMsgSuccess(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                        LibMPC.ButtonVisibility(ButPurchaseApp, false);
                    }
                    else
                    {
                        // App not purchased.
                        LibMPC.OutputMsgError(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                        LibMPC.ButtonVisibility(ButPurchaseApp, true);
                    }
                    PBarStatus.IsIndeterminate = false;
                    PBarStatus.Visibility = Visibility.Collapsed;
                    mainPage.boolPurchaseCheckCompleted = true;
                    mainPage.stringPurchaseCheckOutput = TblkPurchaseApp.Text;
                    await EnablePageItems(true);
                }
            }
        }

        /// <summary>
        /// Attempt to buy application. Method controls visibility/Enable of PBarStatus, TblkPurchaseApp, and ButPurchaseApp.
        /// </summary>
        private async Task AppPurchaseBuy()
        {
            LibMPC.OutputMsgBright(TblkPurchaseApp, mainPage.resourceLoader.GetString("HP_PurchaseBuyMsg"));  // Attempting to purchase application.
            await EnablePageItems(false);
            PBarStatus.Foreground = LibMPC.colorError;          // Set color PBarStatus from default.
            PBarStatus.Visibility = Visibility.Visible;
            PBarStatus.IsIndeterminate = true;
            mainPage.boolAppPurchased = await LibMPC.AppPurchaseBuyAsync(mainPage.applicationDataContainer, mainPage.ds_BoolAppPurchased);
            if (mainPage.boolAppPurchased)
            {
                // App purchased.
                LibMPC.OutputMsgSuccess(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                LibMPC.ButtonVisibility(ButPurchaseApp, false);
            }
            else
            {
                // App not purchased.
                LibMPC.OutputMsgError(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                LibMPC.ButtonVisibility(ButPurchaseApp, true);
            }
            PBarStatus.IsIndeterminate = false;
            PBarStatus.Visibility = Visibility.Collapsed;
            await EnablePageItems(true);
        }

        /// <summary>
        /// If application has not been rated then show ButRateApp occasionally.
        /// </summary>
        private void AppRatedCheck()
        {
            if (!mainPage.boolAppRated)
            {
                if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_IntAppRatedCounter))
                {
                    int intAppRatedCounter = (int)mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter];
                    intAppRatedCounter++;
                    if (intAppRatedCounter >= intShowButRateApp)
                    {
                        // Make ButRateApp visible.
                        //if (mainPage.boolAppPurchased)    // Not needed for this app.
                            //ButRateApp.Margin = new Thickness(16, 0, 16, 16);    // Change margin from (16,0,16,16). Order is left, top, right, bottom.
                        mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = 0;     // Reset data store setting to 0.
                        ButRateApp.Foreground = LibMPC.colorSuccess;
                        LibMPC.ButtonVisibility(ButRateApp, true);
                    }
                    else
                        mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = intAppRatedCounter;     // Update data store setting to intAppRatedCounter.
                }
                else
                    mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = 1;     // Initialize data store setting to 1.
            }
        }

        /// <summary>
        /// Enable items on page if boolEnableItems is true, otherwise disable items on page.
        /// </summary>
        /// <param name="boolEnableItems">If true then enable page items, otherwise disable.</param>
        private async Task EnablePageItems(bool boolEnableItems)
        {
            LibMPC.ButtonIsEnabled(mainPage.mainPageButAbout, boolEnableItems);
            LibMPC.ButtonIsEnabled(mainPage.mainPageButSettings, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButLockerView, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButEncryptLocker, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButDecryptLocker, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButEncryptFile, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButDecryptFile, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButEncryptFolder, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButDecryptFolder, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButDeleteFile, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButDeleteFolder, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButPurchaseApp, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButRateApp, boolEnableItems);
            // Handle following special cases.
            if (boolEnableItems)
                await ButCreateFolderNoArchiveUpdateAsync();    // Enable button only if NoAchive folder does not exist.
            else
                LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, boolEnableItems);    // Disable button. It may already be disabled.
            if (!boolEnableItems && mainPage.boolAppPurchased)
                TblkPurchaseApp.Visibility = Visibility.Collapsed;  // Hide application purchase message after first pass or it will linger until app restart or page change.
        }

        /// <summary>
        /// On exception, stop progress bar, if active, and return exception message.
        /// </summary>
        /// <param name="stringMethodNameException">Name of method that exception occurred in. Sample: "HomePage.EncryptFileArchiveAsync()"</param>
        /// <param name="exception">Exception thrown by method.</param>
        /// <returns></returns>
        private string MessageOutputOnException(string stringMethodNameException, Exception exception)
        {
            // Stop progress bar on exception, if active, or it looks like App is still processing.
            ProgressBarHPShow(false);
            // User should never see this but exit gracefully showing unexpected exception versus letting App crash.
            return mainPage.UnhandledExceptionMessage(stringMethodNameException, exception.GetType());
        }

        /// <summary>
        /// Display item processing message.
        /// </summary>
        /// <param name="iStorageItem">StorageFolder or StorageFile to be processed.</param>
        private void ItemIsBeingProcessedMessage(IStorageItem iStorageItem)
        {
            List<string> list_OutputMsgNormal = new List<string>()
            {
                mainPage.resourceLoader.GetString("HP_ProcessingItem"),
                LockerPathRemove(iStorageItem)      // Variable.
            };
            LibMPC.OutputMsgNormal(TblkResult, LibMPC.JoinListString(list_OutputMsgNormal, EnumStringSeparator.OneSpace));
        }

        /// <summary>
        /// Display page HomePage load message.
        /// </summary>
        private async Task MessageHomePageLoadAsync()
        {
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                // Set and save values since page HomePage loaded.
                mainPage.boolLockerFolderSelected = true;
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolLockerFolderSelected] = true;
                List<string> list_stringMessage = new List<string>()
                {
                    mainPage.resourceLoader.GetString("HP_LockerAvailable"),   // Current locker is
                    mainPage.stringLockerPath
                };
                string stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                if (mainPage.boolSamplesCreated)
                {
                    List<string> list_stringMessage_SampleFiles = new List<string>()
                    {
                        stringMessage,
                        mainPage.resourceLoader.GetString("HP_LockerAvailable_SampleFiles")    // Created sample folders and files in locker to experiment with.  Open one of the files to view application instructions and tips.
                    };
                    stringMessage = LibMPC.JoinListString(list_stringMessage_SampleFiles, EnumStringSeparator.OneNewline);
                    mainPage.boolSamplesCreated = false;    // Show message only once.
                }
                LibMPC.OutputMsgSuccess(TblkResult, stringMessage);
            }
            else
            {
                await EnablePageItems(false);     // Could not find locker folder so disable all butttons preventing user from doing anything else.
                mainPage.boolLockerFolderSelected = false;
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolLockerFolderSelected] = false;
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Update and display content of RichTblkToggleStatus.
        /// RichTblkToggleStatus displays current settings of boolExitApp, boolDeleteSecure, and boolVerbose in error or success colors.
        /// </summary>
        private void UpdateToggleStatus()
        {
            RichTblkToggleStatusParagraph.Inlines.Clear();      // Need to clear default XAML text before updating output string
            Paragraph paragraph = new Paragraph();
            Run modeExitApp = new Run();
            if (boolExitApp)
            {
                modeExitApp.Foreground = LibMPC.colorSuccess;
                modeExitApp.Text = mainPage.resourceLoader.GetString("ST_ModeExitAppOn");
            }
            else
            {
                modeExitApp.Foreground = LibMPC.colorError;
                modeExitApp.Text = mainPage.resourceLoader.GetString("ST_ModeExitAppOff");
            }
            Run separator1 = new Run
            {
                Foreground = LibMPC.colorNormal,
                Text = " - "
            };
            Run modeDeleteSecure = new Run();
            if (boolDeleteSecure)
            {
                modeDeleteSecure.Foreground = LibMPC.colorSuccess;
                modeDeleteSecure.Text = mainPage.resourceLoader.GetString("ST_ModeDeleteSecureOn");
            }
            else
            {
                modeDeleteSecure.Foreground = LibMPC.colorError;
                modeDeleteSecure.Text = mainPage.resourceLoader.GetString("ST_ModeDeleteSecureOff");
            }
            Run separator2 = new Run
            {
                Foreground = LibMPC.colorNormal,
                Text = " - "
            };
            Run modeVerbose = new Run();
            if (mainPage.boolVerbose)
            {
                modeVerbose.Foreground = LibMPC.colorSuccess;
                modeVerbose.Text = mainPage.resourceLoader.GetString("ST_ModeVerboseOn");
            }
            else
            {
                modeVerbose.Foreground = LibMPC.colorError;
                modeVerbose.Text = mainPage.resourceLoader.GetString("ST_ModeVerboseOff");

            }
            paragraph.Inlines.Add(modeExitApp);
            paragraph.Inlines.Add(separator1);
            paragraph.Inlines.Add(modeDeleteSecure);
            paragraph.Inlines.Add(separator2);
            paragraph.Inlines.Add(modeVerbose);
            RichTblkToggleStatus.Blocks.Add(paragraph);
        }

        /// <summary>
        /// Read saved data store setttings mainPage.ds_BoolExitApp and mainPage.ds_BoolDeleteSecure. If they do not exist then create them.
        /// Data store setting mainPage.ds_BoolVerbose is handled in MainPage since used in mutiple pages.
        /// </summary>
        private void ReadDataStoreHPValues()
        {
            // Read or create 'mainPage.ds_BoolExitApp' setting.
            if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_BoolExitApp))
            {
                object objectBoolExitApp = mainPage.applicationDataContainer.Values[mainPage.ds_BoolExitApp];
                if (objectBoolExitApp.Equals(false))
                    boolExitApp = false;
                else
                    boolExitApp = true;
            }
            else    // Did not find 'mainPage.ds_BoolExitApp' setting, so create it.
            {
                boolExitApp = false;
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolExitApp] = false;     // Write setting to data store.
            }
            // Read or create 'mainPage.ds_BoolDeleteSecure' setting.
            if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_BoolDeleteSecure))
            {
                object objectBoolDeleteSecure = mainPage.applicationDataContainer.Values[mainPage.ds_BoolDeleteSecure];
                if (objectBoolDeleteSecure.Equals(false))
                    boolDeleteSecure = false;
                else
                    boolDeleteSecure = true;
            }
            else    // Did not find 'mainPage.ds_BoolDeleteSecure' setting, so create it.
            {
                boolDeleteSecure = false;
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolDeleteSecure] = false;     // Write setting to data store.
            }
        }

        /// <summary>
        /// if parameter boolShow is true then show progress bar and start timer. Otherwise hide progress bar and stop timer.
        /// </summary>
        /// <param name="boolShow"></param>
        private void ProgressBarHPShow(bool boolShow)
        {
            if (boolShow)
            {
                stopWatchTimer.Reset();
                stopWatchTimer.Start();             // Start the timer.
                timeSpanElapsed = TimeSpan.Zero;    // Zero in case any access to value before timer stops.
                PBarStatus.Visibility = Visibility.Visible;
                PBarStatus.IsIndeterminate = true;
            }
            else
            {
                PBarStatus.IsIndeterminate = false;
                PBarStatus.Visibility = Visibility.Collapsed;
                stopWatchTimer.Stop();
                timeSpanElapsed = stopWatchTimer.Elapsed;
                // Debug.WriteLine(String.Format("ProgressBarHPShow(): ProgressBar ran for {0:N2} seconds.", timeSpanElapsed.TotalSeconds));
            }
        }

        /// <summary>
        /// Disable ButCreateFolderNoArchive button if NoArchive folder exists, otherwise enable it.
        /// </summary>
        private async Task ButCreateFolderNoArchiveUpdateAsync()
        {
            if (mainPage.boolLockerFolderSelected)
            {
                IStorageItem iStorageItem = await mainPage.storageFolderLocker.TryGetItemAsync(mainPage.stringFoldernameNoArchive);     // Try get NoArchive folder.
                if (iStorageItem != null)   // Item found but don't know if it was folder or file.
                {
                    if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                    {
                        // NoArchive folder found so disable create folder button.
                        // Debug.WriteLine($"ButCreateFolderNoArchiveUpdateAsync(): NoArchive folder found so disable create folder button. iStorageItem.Path={iStorageItem.Path}");
                        LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, false);
                    }
                    else
                    {
                        // Debug.WriteLine($"ButCreateFolderNoArchiveUpdateAsync(): Item found was not a folder. Cannot create folder while it exists. iStorageItem.Path={iStorageItem.Path}");
                        LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, false);
                    }
                }
                else
                {
                    // NoArchive folder not found so enable create folder button.
                    // Debug.WriteLine($"ButCreateFolderNoArchiveUpdateAsync(): NoArchive folder not found so enable create folder button. iStorageItem was null");
                    LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, true);
                }
            }
            else
            {
                // Locker folder not found so disable create folder button.
                LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, false);
            }
        }

        /// <summary>
        /// Return string made from list of truncated item paths below Locker folder not processed during encryption/decryption method.
        /// </summary>
        /// <param name="listItemPathsNotProcessed">List of item paths not processed. Could be cumulation of errors from mutiple folders.</param>
        /// <returns></returns>
        private string ListItemPathsNotProcessed(List<string> listItemPathsNotProcessed)
        {
            string stringMessageError = string.Empty;
            if (listItemPathsNotProcessed.Count > 0)
            {
                if (listItemPathsNotProcessed.Count == 1)
                {
                    List<string> list_stringMessageError = new List<string>()
                    {
                        $"{Environment.NewLine}{listItemPathsNotProcessed.Count}",       // Variable.
                        mainPage.resourceLoader.GetString("HP_ItemPathsNotProcessedSingle")         // item not processed
                    };
                    stringMessageError = LibMPC.JoinListString(list_stringMessageError, EnumStringSeparator.OneSpace);
                }
                else
                {
                    List<string> list_stringMessageError = new List<string>()
                    {
                        $"{Environment.NewLine}{listItemPathsNotProcessed.Count}",       // Variable.
                        mainPage.resourceLoader.GetString("HP_ItemPathsNotProcessedPlural")         // items not processed
                    };
                    stringMessageError = LibMPC.JoinListString(list_stringMessageError, EnumStringSeparator.OneSpace);
                }
                // Create output string listing files not processed.
                foreach (string stringItem in listItemPathsNotProcessed)
                    stringMessageError += $"{Environment.NewLine}{LockerPathRemove(stringItem)}";   // Add item to string.
            }
            return stringMessageError;
        }

        /// <summary>
        /// Overload method that returns a truncated path string that does not include the Locker path. Exception is return Locker name if path is Locker. 
        /// Parameter checking is done since User can pick a folder from anywhere.
        /// </summary>
        /// <param name="iStorageItem">Folder or file in hierarchy of Locker.</param>
        /// <returns></returns>
        private string LockerPathRemove(IStorageItem iStorageItem)
        {
            if (mainPage.stringLockerPath.Equals(iStorageItem.Path, StringComparison.OrdinalIgnoreCase))    // Need to ignore case.
                return mainPage.storageFolderLocker.Name;
            if (LibUF.ItemInFolderHierarchy(mainPage.stringLockerPath, iStorageItem.Path))
            {
                int intLockerPathLen = mainPage.stringLockerPath.Length + 1;    // Characters to remove to truncate path to below Locker folder.  Also remove leading slash!
                return iStorageItem.Path.Remove(0, intLockerPathLen);           // Return the truncated path.
            }
            return iStorageItem.Path;   // If error, return untruncated path of item.
        }

        /// <summary>
        /// Overload method that returns a truncated path string that does not include the Locker path. Exception is return Locker name if path is to Locker.
        /// Parameter checking is done since User can pick a folder from anywhere.
        /// </summary>
        /// <param name="stringIStorageItemPath">String of folder or file path that includes Locker path.</param>
        /// <returns></returns>
        private string LockerPathRemove(string stringIStorageItemPath)
        {
            if (mainPage.stringLockerPath.Equals(stringIStorageItemPath, StringComparison.OrdinalIgnoreCase))   // Need to ignore case.
                return mainPage.storageFolderLocker.Name;
            int intLockerPathLen = mainPage.stringLockerPath.Length + 1;        // Characters to remove to truncate path to below Locker folder.  Also remove leading slash!
            if (stringIStorageItemPath.Contains(mainPage.stringLockerPath))
            {
                if (stringIStorageItemPath.Length > intLockerPathLen)
                {
                    return stringIStorageItemPath.Remove(0, intLockerPathLen);      // Return the truncated path.
                }
            }
            return stringIStorageItemPath;  // If error, return untruncated path.
        }

        /// <summary>
        /// Check if folder or file is in hierarchy of storageFolderHierarchy or if they are same item. Return true if so, false otherwise.
        /// Also return false if storageFolderHierarchy is null.
        /// </summary>
        /// <param name="storageFolderHierarchy">Top level storagefolder that sets hierarchy path.</param>
        /// <param name="iStorageItem">Folder or file to check if in hierarchy of storageFolderHierarchy.</param>
        /// <returns></returns>
        private bool CheckHierarchy(StorageFolder storageFolderHierarchy, IStorageItem iStorageItem)
        {
            try
            {
                if (storageFolderHierarchy == null)     // NoArchive folder will be null if does not exist.
                {
                    // Debug.WriteLine($"HomePage.CheckHierarchy(): storageFolderHierarchy is null so returned false");
                    return false;
                }
                string stringItemPath = iStorageItem.Path;
                string stringrHierarchyPath = storageFolderHierarchy.Path;
                // Debug.WriteLine($"HomePage.CheckIfInHierarchyAsync():       stringItemPath={stringItemPath}");
                // Debug.WriteLine($"HomePage.CheckHierarchy():          stringrHierarchyPath={stringrHierarchyPath}");
                //
                // Error condition sample: C:\Data\Users\Public\Documents\... or C:\Data\USERS\Public\Documents\...
                // Sometimes either format is returned causing comparision to fail and erratic behavior afterwards...
                // https://docs.microsoft.com/en-us/dotnet/api/system.stringcomparison?view=netframework-4.7.2
                // https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings?view=netframework-4.7.2
                //
                // Compare if paths are same in case where User selected the NoArchive folder versus something in it.
                if (stringItemPath.Equals(stringrHierarchyPath, StringComparison.OrdinalIgnoreCase))    // Need to ignore case.
                {
                    // Debug.WriteLine($"HomePage.CheckHierarchy(): Items are same so returned true");
                    return true;
                }
                // Need to append a backslash to storageFolderHierarchy.Path so can filter out folders that start with same name.
                stringrHierarchyPath = $"{storageFolderHierarchy.Path}{Path.DirectorySeparatorChar}";
                // Debug.WriteLine($"HomePage.CheckHierarchy(): Adjusted stringrHierarchyPath={stringrHierarchyPath}");
                // Debug code:
                //bool boolPathCompare = stringItemPath.StartsWith(stringrHierarchyPath, StringComparison.OrdinalIgnoreCase);     // Need to ignore case.
                //Debug.WriteLine($"HomePage.CheckHierarchy(): Returned boolPathCompare={boolPathCompare}");
                //return boolPathCompare;
                // Clean code:
                return stringItemPath.StartsWith(stringrHierarchyPath, StringComparison.OrdinalIgnoreCase);     // Need to ignore case.
            }
            catch
            {
                return false;   // Something went wrong so return false.
            }
        }

        /// <summary>
        /// Returns StorageFolder equivalent to storageFolderPicked if success, null otherwise.
        /// StorageFolder returned by FolderPicker does not allow App to delete picked folder even if App has access to folder above it. 
        /// This is a programmatic work-a-round to get get access to picked folder.
        /// This will also return null if folder is not in hierarchy of Locker folder.
        /// </summary>
        /// <param name="storageFolderPicked">StorageFolder obtained from folder picker.</param>
        /// <returns></returns>
        private async Task<StorageFolder> StorageFolderFromFolderPickerAsync(StorageFolder storageFolderPicked)
        {
            try
            {
                // Check if storageFolderPicked has read and write access to parent folder. Continue if so.
                StorageFolder storageFolderParent = await storageFolderPicked.GetParentAsync();     // GetParentAsync() will return null if App does not have access to it!
                if (storageFolderParent != null)
                {
                    IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(storageFolderPicked.Name);
                    if (iStorageItem != null)   // Item found but don't know if it was folder or file.
                    {
                        if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                        {
                            // Returned StorageFolder is programmatically equivalent to storageFolderPicked and App can now delete it.
                            return (StorageFolder)iStorageItem;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get EnumModeDelete value.  Boolean HomePage.boolDeleteSecure set in Settings page.
        /// </summary>
        /// <returns></returns>
        private EnumModeDelete GetEnumModeDelete()
        {
            if (boolDeleteSecure)
                return EnumModeDelete.DeleteSecure;
            else
                return EnumModeDelete.DeleteNormal;
        }

        /// <summary>
        /// Return true if storageFileToCheck has encrypted FileType corresponding to enumEncryptedFileTypeCompareMode value, otherwise return false.
        /// </summary>
        /// <param name="storageFileToCheck">StorageFile to check if encrypted.</param>
        /// <param name="enumEncryptedFileTypeCompareMode">Encrypted FileType Compare Mode.</param>
        /// <param name="stringEncryptedFileTypeToSkip">Encrypted FileType to use in comparision. Can be null if not used in comparision.</param>
        /// <returns></returns>
        private bool StorageFileIsEncrypted(StorageFile storageFileToCheck, EnumEncryptedFileTypeCompareMode enumEncryptedFileTypeCompareMode, string stringEncryptedFileTypeCompare = null)
        {
            // Generally, through App, test NoArchive conditions first since likely to have many more files to process. Get out quick!
            string stringFileType = storageFileToCheck.FileType;
            switch (enumEncryptedFileTypeCompareMode)
            {
                case EnumEncryptedFileTypeCompareMode.NoArchive:
                    if (stringFileType.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))     // ".aes"
                        return true;
                    break;
                case EnumEncryptedFileTypeCompareMode.Archive:
                    if (stringFileType.Equals(mainPage.stringExtensionArchive, StringComparison.OrdinalIgnoreCase))       // ".arc"
                        return true;
                    break;
                case EnumEncryptedFileTypeCompareMode.All:
                    if (stringFileType.Equals(mainPage.stringExtensionArchive, StringComparison.OrdinalIgnoreCase))       // ".arc"
                        return true;
                    if (stringFileType.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))     // ".aes"
                        return true;
                    break;
                case EnumEncryptedFileTypeCompareMode.AllButFileTypeToSkip:
                    if (stringEncryptedFileTypeCompare.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))   // Skip NoArchive FileType of ".aes"
                    {
                        if (stringFileType.Equals(mainPage.stringExtensionArchive, StringComparison.OrdinalIgnoreCase))     // ".arc"
                            return true;
                    }
                    if (stringEncryptedFileTypeCompare.Equals(mainPage.stringExtensionArchive, StringComparison.OrdinalIgnoreCase))     // Skip Archive FileType of ".arc"
                    {
                        if (stringFileType.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))   // ".aes"
                            return true;
                    }
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"HomePage.StorageFileIsEncrypted(): enumEncryptedFileTypeCompareMode={enumEncryptedFileTypeCompareMode} not found in switch statement.");
            }
            return false;
        }

        /// <summary>
        /// Encrypt storageFileToEncrypt by placing it in a wrapper folder, then archive wrapper folder to a file, then encrypt file.
        /// storageFileToEncrypt must be in hierarchy of Locker folder but not in NoArchive folders.
        /// Calling method must check storageFileToEncrypt is not encrypted Archive FileType or locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder of storageFileToEncrypt.</param>
        /// <param name="storageFileToEncrypt">Locker file to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFileArchiveAsync(StorageFolder storageFolderParent, StorageFile storageFileToEncrypt)
        {
            string stringMessage = string.Empty;
            string stringFoldernameWrapper = null;
            bool boolSuccess = false;   // Initialize to false.  Set to true when file is encrypted without an error.
            try
            {
                // Calling method must check storageFileToEncrypt is not encrypted Archive FileType or locked since no checks done by this method.
                bool boolContinue = true;   // Initialize to true.  Set to false if error decrypting storageFileToEncrypt, if encrypted.
                string stringFilename = LockerPathRemove(storageFileToEncrypt);     // Save truncated path filename for output.
                // Check if storageFileToEncrypt is encrypted. Calling methods abort if FileType is mainPage.stringExtensionArchive.
                // So if file is encrypted, then FileType is incorrect for Locker folder not in NoArchive folder.
                // Need to decrypt file and then encrypt it again to convert file to proper FileType. Also don't want to encrypt file second time.
                if (StorageFileIsEncrypted(storageFileToEncrypt, EnumEncryptedFileTypeCompareMode.AllButFileTypeToSkip, mainPage.stringExtensionArchive))
                {
                    // storageFileToEncrypt is encrypted and has improper FileType, need to decrypt it.
                    // Remove file extension from end of storageFileToDycrypt.
                    string stringFilenameDecrypted = Path.GetFileNameWithoutExtension(storageFileToEncrypt.Name);
                    // if mainPage.stringExtensionArchive=".arc", then FileType is ".aes".
                    // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Attempting to decrypt {stringFilename}, mainPage.stringExtensionArchive={mainPage.stringExtensionArchive}, stringFilenameDecrypted={stringFilenameDecrypted}");
                    if (await DecryptFileAsync(storageFolderParent, storageFileToEncrypt))
                    {
                        // Decrypted storageFileToEncrypt successfully so keep going without pause. Do not show User success message.
                        // Note: Decrypted file could be a folder. Need to check.
                        // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Decrypted {stringFilename}");
                        IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(stringFilenameDecrypted);
                        if (iStorageItem != null)     // Item found but don't know if it was folder or file.
                        {
                            if (iStorageItem.IsOfType(StorageItemTypes.File))
                            {
                                // Need to retrieve newly decrypted file into storageFileToEncrypt so it can now be encrypted.
                                storageFileToEncrypt = (StorageFile)iStorageItem;
                                // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): New name of storageFileToEncrypt={storageFileToEncrypt.Name}");
                            }
                            if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                            {
                                boolContinue = false;   // Need to skip rest of this method since decrypted item is a folder.
                                // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Decrypted item is a folder so set boolContinue={boolContinue}.");
                                // Need to retrieve newly decrypted folder into storageFileToEncrypt so it can now be encrypted.
                                StorageFolder storageFolderToEncrypt = (StorageFolder)iStorageItem;
                                if (await EncryptFolderArchiveAsync(storageFolderParent, storageFolderToEncrypt))
                                {
                                    // Encrypted folder.
                                    stringMessage = stringMessageResult;
                                    boolSuccess = true;
                                    // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Encrypted storageFolderToEncrypt={storageFolderToEncrypt.Name}");
                                }
                                else
                                {
                                    // Could not encrypt folder.
                                    stringMessage = stringMessageResult;
                                    boolSuccess = false;
                                    // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Could not encrypt storageFolderToEncrypt={storageFolderToEncrypt.Name}");
                                }
                            }
                        }
                        else
                        {
                            boolContinue = false;
                            // Could not retrieve decrypted storageFileToDecrypt.
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt"),      // Could not decrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt_ProcessFail")   // since decryption process failed
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                    }
                    else
                    {
                        // Could not decrypt storageFileToEncrypt so abort and show User error message.
                        boolContinue = false;
                        stringMessage = stringMessageResult;
                    }
                }
                if (boolContinue)
                {
                    // storageFileToEncrypt is not encrypted, so encrypt it.
                    // Debug.WriteLine($"HomePage.EncryptFileArchiveAsync(): Attempting to encrypt {LockerPathRemove(storageFileToEncrypt)}, mainPage.stringExtensionArchive={mainPage.stringExtensionArchive}");
                    string stringFilenameEncrypted = $"{storageFileToEncrypt.Name}{mainPage.stringExtensionArchive}";
                    if (await storageFolderParent.TryGetItemAsync(stringFilenameEncrypted) == null)     // Abort if item with same name already exists.
                    {
                        // Create wrapper folder to place file into.
                        stringFoldernameWrapper = LibAES.StringGuidTempFoldername();
                        StorageFolder storageFolderWrapper = await storageFolderParent.CreateFolderAsync(stringFoldernameWrapper);
                        if (storageFolderWrapper != null)
                        {
                            // Create flag file inside wrapper folder.
                            StorageFile storageFileFlag = await storageFolderWrapper.CreateFileAsync(stringFilenameFlag);
                            if (storageFileFlag != null)
                            {
                                // If storageFileToEncrypt is locked then next line will throw System.UnauthorizedAccessException.
                                await storageFileToEncrypt.MoveAsync(storageFolderWrapper);         // Move storageFileToEncrypt to wrapper folder.
                                // File lock check done by calling method so set parameter boolCheckIfLocked to false so lock check not done again.
                                // Archive and encrypt storageFolderWrapper using random IV and embed random IV in output file. Output file has extension of ".arc".
                                StorageFile storageFileEncrypted = await LibAES.StorageFolderCompressEncryptAsync(storageFolderParent, storageFolderWrapper, mainPage.stringExtensionArchive, mainPage.cryptographicKeyAppPassword, LibAES.IBufferIVRandom(), GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                                if (storageFileEncrypted != null)
                                {
                                    // Success!  storageFolderWrapper was archived and output file is encrypted.
                                    await storageFileEncrypted.RenameAsync(stringFilenameEncrypted);    // Rename wrapper folder.
                                    boolSuccess = true;
                                    List<string> list_stringMessage = new List<string>()
                                    {
                                        mainPage.resourceLoader.GetString("HP_Success_Encrypt"),    // Encrypted
                                        stringFilename,
                                    };
                                    stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                                }
                                else
                                {   // Encryption failed.  Clean up by moving storageFileToEncrypt back to orginal location and then deleting wrapper folder and flag file inside it.
                                    await storageFileToEncrypt.MoveAsync(storageFolderParent);
                                    await storageFolderWrapper.DeleteAsync(StorageDeleteOption.PermanentDelete);
                                    List<string> list_stringMessage = new List<string>()
                                    {
                                        mainPage.resourceLoader.GetString("HP_Error_Encrypt"),  // Could not encrypt
                                        stringFilename,
                                        mainPage.resourceLoader.GetString("HP_Error_Encrypt_ProcessFail")   // since encryption process failed
                                    };
                                    stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                                }
                            }
                            else
                            {
                                // Need to clean up by deleting the wrapper folder.
                                await storageFolderWrapper.DeleteAsync(StorageDeleteOption.PermanentDelete);
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),  // Could not encrypt
                                    stringFilename,
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt_FlagFile")  // since could not create flag file
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                            }
                        }
                        else
                        {
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt"),  // Could not encrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt_WrapperFolder") // since could not create wrapper folder
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                    }
                    else
                    {
                        // Could not encrypt {0} since {1} already contains item with name {2}  <-- Note: Translate whole phrase!
                        stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Encrypt_ItemExistDest"), stringFilename, LockerPathRemove(storageFolderParent), stringFilenameEncrypted);
                    }
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                // If exception then check if wrapper folder exists. May need to move storageFileToEncrypt back to parent folder and delete wrapper folder if so.
                if (stringFoldernameWrapper != null)    // Wrapper folder name was created so continue.
                {
                    IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(stringFoldernameWrapper);
                    if (iStorageItem != null)  // Found wrapper folder so continue.
                    {
                        StorageFolder storageFolderWrapperException = (StorageFolder)iStorageItem;  // StorageFolder cast is safe without checking since know it is a folder.
                        iStorageItem = await storageFolderWrapperException.TryGetItemAsync(storageFileToEncrypt.Name);
                        if (iStorageItem != null)  // Found original file to encrypt in wrapper folder so move it back to parent folder.
                        {
                            StorageFile storageFileToEncryptException = (StorageFile)iStorageItem;  // StorageFile cast is safe without checking since know it is a file.
                            await storageFileToEncryptException.MoveAsync(storageFolderParent);     // Move storageFileToEncrypt back to parent folder since exception.
                        }
                        await storageFolderWrapperException.DeleteAsync(StorageDeleteOption.PermanentDelete);   // No need to secure delete since nothing useful saved here.
                    }
                }
                stringMessage = MessageOutputOnException("HomePage.EncryptFileArchiveAsync()", ex);     // Show exception message.
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Encrypt storageFolderToEncrypt by archiving into a file and then encrypting file.
        /// storageFolderToEncrypt must be in hierarchy of Locker folder but not in NoArchive folder.
        /// Calling method must check storageFolderToEncrypt is not locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder of storageFolderToEncrypt.</param>
        /// <param name="storageFolderToEncrypt">Folder to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFolderArchiveAsync(StorageFolder storageFolderParent, StorageFolder storageFolderToEncrypt)
        {
            string stringMessage = string.Empty;
            bool boolSuccess = false;   // Initialize to false. Set to true when item is encrypted without an error.
            try
            {
                // Calling method must check storageFolderToEncrypt is not locked since no checks done by this method.
                string stringFoldername = LockerPathRemove(storageFolderToEncrypt);    // Save truncated path foldername for output since storageFolderToEncrypt will be deleted.
                bool boolContinue = true;   // Initialize to true.  Set to false if error decrypting storageFolderToEncrypt, if encrypted.
                // Decrypt encrypted files in storageFolderToEncrypt that have FileType that does not match mainPage.stringExtensionArchive.
                // Set compare mode to use in DecryptFolderAsync() called below to decrypt all encypted files but those that have FileType of mainPage.stringExtensionArchive.
                enumEncryptedFileTypeCompareModeRecursive = EnumEncryptedFileTypeCompareMode.AllButFileTypeToSkip;
                // Do not decrypt files with FileType of mainPage.stringExtensionArchive since already encrypted properly.
                stringEncryptedFileTypeCompareRecursive = mainPage.stringExtensionArchive;
                intFilesFoundRecursive = 0;             // Zero counter.
                boolProcessSuccessRecursive = true;     // Remains true if no decryption errors.
                listFileErrorsRecursive.Clear();        // Reset/Clear list of file path errors from previous runs.
                await DecryptFolderAsync(storageFolderToEncrypt);   // Recursive method that decrypts files of wrong FileType.
                // If decrypted encrypted files with wrong FileType in storageFolderToEncrypt then keep going without pause and do not show User success message.
                if (!boolProcessSuccessRecursive)
                {
                    // Could not decypt at least one encrypted file with wrong FileType in storageFolderToEncrypt.
                    boolContinue = false;
                    // Decrypted {0} files in {1}  <-- Note: Translate whole phrase!
                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FolderFilesFailed"), intFilesFoundRecursive - listFileErrorsRecursive.Count, stringFoldername);
                    stringMessage += ListItemPathsNotProcessed(listFileErrorsRecursive);    // items not processed
                }
                if (boolContinue)   // Encrypt storageFolderToEncrypt.
                {
                    // Debug.WriteLine($"HomePage.EncryptFolderArchiveAsync(): Attempting to encrypt {LockerPathRemove(storageFolderToEncrypt)}, mainPage.stringExtensionArchive={mainPage.stringExtensionArchive}");
                    string stringFilenameEncrypted = $"{storageFolderToEncrypt.Name}{mainPage.stringExtensionArchive}";
                    if (await storageFolderParent.TryGetItemAsync(stringFilenameEncrypted) == null)       // Abort if file with same name already exists.
                    {
                        // Folder lock check done by calling method so set parameter boolCheckIfLocked to false so lock check not done again.
                        StorageFile storageFileEncrypted = await LibAES.StorageFolderCompressEncryptAsync(storageFolderParent, storageFolderToEncrypt, mainPage.stringExtensionArchive, mainPage.cryptographicKeyAppPassword, LibAES.IBufferIVRandom(), GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                        if (storageFileEncrypted != null)
                        {
                            // Success!  storageFolderToEncrypt was archived and output file is encrypted.
                            boolSuccess = true;
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Success_Encrypt"),    // Encrypted
                                stringFoldername
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                        else
                        {
                            // Fail!  Folder was not encrypted.
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                stringFoldername,
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt_ProcessFail")   // since encryption process failed
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                    }
                    else
                    {
                        // Could not encrypt {0} since {1} already contains item with name {2}  <-- Note: Translate whole phrase!
                        stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Encrypt_ItemExistDest"), stringFoldername, LockerPathRemove(storageFolderParent), stringFilenameEncrypted);
                    }
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                stringMessage = MessageOutputOnException("HomePage.EncryptFolderArchiveAsync()", ex);
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Decrypt storageFileToDecrypt back to original folder or file. Return true if success, false otherwise.
        /// Calling method must check storageFileToDecrypt is encrypted and not locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder of storageFileToDecrypt.</param>
        /// <param name="storageFileToDecrypt">File to be decrypted.</param>
        /// <returns></returns>
        private async Task<bool> DecryptFileArchiveAsync(StorageFolder storageFolderParent, StorageFile storageFileToDecrypt)
        {
            string stringMessage = string.Empty;
            bool boolSuccess = false;   // Initialize to false.  Set to true when item is decrypted without an error.
            try
            {
                // Calling method must check storageFileToDecrypt is encrypted and not locked since no checks done by this method.
                string stringFilename = LockerPathRemove(storageFileToDecrypt);    // Save truncated path filername for output.
                // Remove last file extension. stringItemnameDecrypted can be name of folder or file.
                string stringItemnameDecrypted = Path.GetFileNameWithoutExtension(storageFileToDecrypt.Name);
                // Check if folder or file matching decrypted name already exists.
                if (await storageFolderParent.TryGetItemAsync(stringItemnameDecrypted) == null)
                {
                    // File lock check done by calling method so set parameter boolCheckIfLocked to false so lock check not done again.
                    StorageFolder storageFolderDest = null;
                    if (storageFileToDecrypt.FileType.Equals(mainPage.stringExtensionArchive, StringComparison.OrdinalIgnoreCase))
                    {
                        // File extension is ".arc" so decrypt and extract storageFileToProcess using embedded random IV.
                        storageFolderDest = await LibAES.StorageFileDecryptExtractAsync(storageFolderParent, storageFileToDecrypt, stringItemnameDecrypted, mainPage.cryptographicKeyAppPassword, null, GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                    }
                    // If invalid file extension after above checks, or file did not decrypt, then storageFolderDest is still null and method will end showing error.
                    if (storageFolderDest != null)
                    {
                        // Successfully decrypted storageFileToDecrypt to storageFolderDest.
                        // Calling methods control timer and progress bar and choose to show success message.
                        // This method will return success message versus empty string as typical with other methods since decrytping folder or file.
                        IReadOnlyList<StorageFile> listStorageFiles = await storageFolderDest.GetFilesAsync();   // Get list of files in folder.
                        if (listStorageFiles != null)
                        {
                            bool boolFlagFileFound = false;
                            string stringFilenameOriginalFile = string.Empty;
                            if (listStorageFiles.Count == 2)     // Check if decrypted folder contains two files. Searching for flag file which indicates other file is original file.
                            {
                                string StringFilename0 = listStorageFiles[0].Name;
                                string stringFilename1 = listStorageFiles[1].Name;
                                if (StringFilename0.Equals(stringFilenameFlag, StringComparison.OrdinalIgnoreCase))
                                {
                                    stringFilenameOriginalFile = stringFilename1;  // Found flag file so get filename of other file in folder. This filename is name of original file.
                                    boolFlagFileFound = true;
                                }
                                else if (stringFilename1.Equals(stringFilenameFlag, StringComparison.OrdinalIgnoreCase))
                                {
                                    stringFilenameOriginalFile = StringFilename0;  // Found flag file so get filename of other file in folder. This filename is name of original file.
                                    boolFlagFileFound = true;
                                }
                            }
                            if (boolFlagFileFound)  // If flag file was not found, then storageFolderDest has correct name of original folder.
                            {
                                await storageFolderDest.RenameAsync(LibAES.StringGuidTempFoldername());     // Rename destination folder to temp flag foldername.
                                StorageFile storagFileFound = (StorageFile)await storageFolderDest.TryGetItemAsync(stringFilenameOriginalFile);  // Get decrpted file using name found earlier.  Cast is safe since know it is a file.
                                if (storagFileFound != null)
                                {
                                    // Success! boolFlagFileFound is true, therfore decrypted a file that was in a wrapper folder.
                                    await storagFileFound.MoveAsync(storageFolderParent);   // Move file back to parent folder from Zip wrapper folder.
                                    await storageFolderDest.DeleteAsync(StorageDeleteOption.PermanentDelete);   // Delete Zip wrapper folder and flag file inside it.
                                    boolSuccess = true;
                                    List<string> list_stringMessage = new List<string>()
                                    {
                                        mainPage.resourceLoader.GetString("HP_Success_Decrypt"),    // Decrypted
                                        stringFilename
                                    };
                                    stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                                }
                                else
                                {
                                    // Decrypted {0} but could not get {1}, decrypted file was left in folder {2}
                                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FileGetFailed"), stringFilename, stringFilenameOriginalFile, storageFolderDest.Name);
                                }
                            }
                            else
                            {
                                // Success! boolFlagFileFound is false, therfore decrypted a folder versus a file that was enclosed in a wrapper folder.
                                boolSuccess = true;
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Success_Decrypt"),    // Decrypted
                                    stringFilename
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                            }
                        }
                        else
                        {
                            // Decrypted {0} but could not get list of files, decrypted content is in {1}   <-- Note: Translate whole phrase!
                            stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FileListFailed"), stringFilename, LockerPathRemove(storageFolderDest));
                        }
                    }
                    else
                    {
                        List<string> list_stringMessage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt"),  // Could not decrypt
                            stringFilename,
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt_ProcessFail")   // since decryption process failed
                        };
                        stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                    }
                }
                else
                {
                    // Could not decrypt {0} since {1} already contains item with name {2}  <-- Note: Translate whole phrase!
                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_ItemExistDest"), stringFilename, LockerPathRemove(storageFolderParent), stringItemnameDecrypted);
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                stringMessage = MessageOutputOnException("HomePage.DecryptFileArchiveAsync()", ex);
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Encrypt storageFileToEncrypt in hierarchy of mainPage.stringFoldernameNoArchive.
        /// Calling method must check storageFileToEncrypt is not encrypted NoArchive FileType or locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder of storageFileToEncrypt.</param>
        /// <param name="storageFileToEncrypt">File to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFileNoArchiveAsync(StorageFolder storageFolderParent, StorageFile storageFileToEncrypt)
        {
            string stringMessage = string.Empty;
            bool boolSuccess = false;   // Initialize to false.  Set to true when file is encrypted without an error.
            try
            {
                // Calling method must check storageFileToEncrypt is not encrypted NoArchive FileType or locked since no checks done by this method.
                bool boolContinue = true;   // Initialize to true.  Set to false if error decrypting storageFileToEncrypt, if encrypted.
                string stringFilename = LockerPathRemove(storageFileToEncrypt);     // Save truncated path filename for output.
                // Check if storageFileToEncrypt is encrypted. Calling methods abort if FileType is mainPage.stringExtensionNoArchive.
                // So if file is encrypted, then FileType is incorrect for NoArchive folder. 
                // Need to decrypt file and then encrypt it again to convert file to proper FileType. Also don't want to encrypt file second time.
                if (StorageFileIsEncrypted(storageFileToEncrypt, EnumEncryptedFileTypeCompareMode.AllButFileTypeToSkip, mainPage.stringExtensionNoArchive))
                {
                    // storageFileToEncrypt is encrypted and has improper FileType, need to decrypt it.
                    // Remove file extension from end of storeageFileToDycrypt.
                    string stringFilenameDecrypted = Path.GetFileNameWithoutExtension(storageFileToEncrypt.Name);
                    // Debug.WriteLine($"HomePage.EncryptFileNoArchiveAsync(): Attempting to decrypt {stringFilename}, mainPage.stringExtensionNoArchive={mainPage.stringExtensionNoArchive}, stringFilenameDecrypted={stringFilenameDecrypted}");
                    if (await DecryptFileAsync(storageFolderParent, storageFileToEncrypt))
                    {
                        // Decrypted storageFileToEncrypt successfully so keep going without pause. Do not show User success message.
                        // Debug.WriteLine($"HomePage.EncryptFileNoArchiveAsync(): Decrypted {stringFilename}");
                        IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(stringFilenameDecrypted);
                        if (iStorageItem != null)     // Item found but don't know if it was folder or file.
                        {
                            if (iStorageItem.IsOfType(StorageItemTypes.File))
                            {
                                // Need to retrieve newly decrypted file into storageFileToEncrypt so it can now be encrypted.
                                storageFileToEncrypt = (StorageFile)iStorageItem;
                                // Debug.WriteLine($"HomePage.EncryptFileNoArchiveAsync(): New name of storageFileToEncrypt={storageFileToEncrypt.Name}");
                            }
                        }
                        else
                        {
                            boolContinue = false;
                            // Could not retrieve decrypted storageFileToDecrypt.
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt"),  // Could not decrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt_ProcessFail")   // since decryption process failed
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                    }
                    else
                    {
                        // Could not decrypt storageFileToEncrypt so abort and show User error message.
                        boolContinue = false;
                        stringMessage = stringMessageResult;
                    }
                }
                if (boolContinue)
                {
                    // storageFileToEncrypt is not encrypted, so encrypt it.
                    // Debug.WriteLine($"HomePage.EncryptFileNoArchiveAsync(): Attempting to encrypt {LockerPathRemove(storageFileToEncrypt)}, mainPage.stringExtensionNoArchive={mainPage.stringExtensionNoArchive}");
                    string stringFilenameEncrypted = $"{storageFileToEncrypt.Name}{mainPage.stringExtensionNoArchive}";
                    if (await storageFolderParent.TryGetItemAsync(stringFilenameEncrypted) == null)     // Abort if item with same name already exists.
                    {
                        // File lock check was done by calling method so set parameter boolCheckIfLocked to false so lock check is not done again.
                        // Encrypt storageFileToEncrypt using random IV and embed random IV in output file. Output file has extension of ".aes".
                        StorageFile storageFileEncrypted = await LibAES.StorageFileEncryptAsync(storageFolderParent, storageFileToEncrypt, mainPage.stringExtensionNoArchive, mainPage.cryptographicKeyAppPassword, LibAES.IBufferIVRandom(), GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                        if (storageFileEncrypted != null)
                        {
                            // Success! Encrypted storageFileToEncrypt.
                            boolSuccess = true;
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Success_Encrypt"),    // Encrypted
                                stringFilename
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                        else
                        {
                            // Fail! Could not encrypt storageFileToEncrypt.
                            List<string> list_stringMessage = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt_ProcessFail")   // since encryption process failed
                            };
                            stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                        }
                    }
                    else
                    {
                        // Could not encrypt {0} since {1} already contains item with name {2}  <-- Note: Translate whole phrase!
                        stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Encrypt_ItemExistDest"), stringFilename, LockerPathRemove(storageFolderParent), stringFilenameEncrypted);
                    }
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                stringMessage = MessageOutputOnException("HomePage.EncryptFileNoArchiveAsync()", ex);
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Decrypt storageFileToDecrypt in hierarchy of mainPage.stringFoldernameNoArchive. Return true if success, false otherwise.
        /// Calling method must check storageFileToDecrypt is encrypted and not locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder of storageFileToDecrypt.</param>
        /// <param name="storageFileToDecrypt">File to decrypt.</param>
        /// <returns></returns>
        private async Task<bool> DecryptFileNoArchiveAsync(StorageFolder storageFolderParent, StorageFile storageFileToDecrypt)
        {
            string stringMessage = string.Empty;
            bool boolSuccess = false;   // Initialize to false.  Set to true when file is decrypted without an error.
            try
            {
                // Calling method must check storageFileToDecrypt is encrypted and not locked since no checks done by this method.
                string stringFilename = LockerPathRemove(storageFileToDecrypt);    // Save truncated path filename for output.
                // Remove file extension from end of storeageFileToDycrypt.
                string stringFilenameDecrypted = Path.GetFileNameWithoutExtension(storageFileToDecrypt.Name);
                // Check if decrypted item already exists.
                if (await storageFolderParent.TryGetItemAsync(stringFilenameDecrypted) == null)     // Abort if item with same name already exists.
                {
                    // File lock check was done by calling method so set parameter boolCheckIfLocked to false so lock check is not done again.
                    StorageFile storageFileDecrypted = null;
                    if (storageFileToDecrypt.FileType.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))
                    {
                        // File extension is ".aes" so extract embedded random IV and use it to decrypt storageFileToProcess.
                        storageFileDecrypted = await LibAES.StorageFileDecryptAsync(storageFolderParent, storageFileToDecrypt, mainPage.cryptographicKeyAppPassword, null, GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                    }
                    // If invalid file extension after above checks, or file did not decrypt, then storageFileDecrypted is still null and method will exit showing error.
                    if (storageFileDecrypted != null)
                    {
                        // Success! Decrypted storageFileToDecrypt.
                        // Debug.WriteLine($"HomePage.DecryptFileNoArchiveAsync(): Decrypted stringFilename={stringFilename} to new storageFileDecrypted.Path={storageFileDecrypted.Path}");
                        boolSuccess = true;
                        List<string> list_stringMessage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Success_Decrypt"),    // Decrypted
                            stringFilename
                        };
                        stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                    }
                    else
                    {
                        // Fail! Could not decrypt storageFileToDecrypt.
                        List<string> list_stringMessage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt"),  // Could not decrypt
                            stringFilename,
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt_ProcessFail")   // since decryption process failed
                        };
                        stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                    }
                }
                else
                {
                    // Could not decrypt {0} since {1} already contains item with name {2}  <-- Note: Translate whole phrase!
                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_ItemExistDest"), stringFilename, LockerPathRemove(storageFolderParent), stringFilenameDecrypted);
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                stringMessage = MessageOutputOnException("HomePage.DecryptFileNoArchiveAsync()", ex);
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Encrypt files in folder that is in hierarchy of mainPage.stringFoldernameNoArchive.
        /// Calling method must check storageFolderToEncrypt is not locked since no checks done by this method.
        /// </summary>
        /// <param name="storageFolderToEncrypt">Folder to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFolderNoArchiveAsync(StorageFolder storageFolderToEncrypt)
        {
            string stringMessage = string.Empty;
            bool boolSuccess = false;   // Initialize to false.  Set to true when item is encrypted without an error.
            try
            {
                // Calling method must check storageFolderToEncrypt is not locked since no checks done by this method.
                bool boolContinue = true;   // Initialize to true.  Set to false if error decrypting storageFolderToEncrypt, if encrypted.
                string stringFoldername = LockerPathRemove(storageFolderToEncrypt);     // Save truncated path foldername for output.
                // Decrypt encrypted files in storageFolderToEncrypt that have FileType that does not match mainPage.stringExtensionNoArchive or the encrypted files will be encrypted again.
                // Set compare mode to use in DecryptFolderAsync() called below to decrypt all encypted files but those that have FileType of mainPage.stringExtensionNoArchive.
                enumEncryptedFileTypeCompareModeRecursive = EnumEncryptedFileTypeCompareMode.AllButFileTypeToSkip;
                // Do not decrypt files with FileType of mainPage.stringExtensionNoArchive since already encrypted properly.
                stringEncryptedFileTypeCompareRecursive = mainPage.stringExtensionNoArchive;
                intFilesFoundRecursive = 0;             // Zero counter.
                boolProcessSuccessRecursive = true;     // Remains true if no decryption errors.
                listFileErrorsRecursive.Clear();        // Reset/Clear list of file path errors from previous runs.
                await DecryptFolderAsync(storageFolderToEncrypt);   // Recursive method that decrypts files of wrong FileType before encrypting them again.
                if (!boolProcessSuccessRecursive)
                {
                    // Could not decypt at least one encrypted file with wrong FileType in storageFolderToEncrypt before encrypting again.
                    boolContinue = false;
                    // Decrypted {0} files in {1}  <-- Note: Translate whole phrase!
                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FolderFilesFailed"), intFilesFoundRecursive - listFileErrorsRecursive.Count, stringFoldername);
                    stringMessage += ListItemPathsNotProcessed(listFileErrorsRecursive);    // items not processed
                }
                else
                {
                    // Decrypted encrypted files with wrong FileType in storageFolderToEncrypt successfully so keep going without pause. Do not show User success message.
                }
                if (boolContinue)
                {
                    // Encrypt storageFolderToEncrypt.
                    // Debug.WriteLine($"HomePage.EncryptFolderNoArchiveAsync(): Attempting to encrypt {LockerPathRemove(storageFolderToEncrypt)}, mainPage.stringExtensionNoArchive={mainPage.stringExtensionNoArchive}");
                    // Folder lock check was done by calling method so set parameter boolCheckIfLocked to false so lock check is not done again.
                    // Encrypt files in storageFolderToEncrypt using random IV and embed random IV in output files. Output files have extension of ".aes".
                    bool boolEncryptResult = await LibAES.StorageFolderEncryptAsync(storageFolderToEncrypt, mainPage.stringExtensionNoArchive, mainPage.cryptographicKeyAppPassword, LibAES.IBufferIVRandom(), GetEnumModeDelete(), EnumModeIV.EmbedIV, false);
                    if (boolEncryptResult)  // True if all files successfully encrypted, false otherwise.
                    {
                        // Success! All files in storageFolderToEncrypt successfully encrypted.
                        boolSuccess = true;
                        List<string> list_stringMessage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Success_Encrypt"),    // Encrypted
                            stringFoldername
                        };
                        stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                    }
                    else
                    {
                        // Fail!  At least one file in storageFolderToEncrypt was not encrypted.  Show user list of files that did not encrypt.
                        // Encrypted {0} files in {1}  <-- Note: Translate whole phrase!
                        stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Encrypt_FolderFilesFailed"), LibAES.intCounterItemsRecursive - LibAES.listFilePathErrorsRecursive.Count, stringFoldername);
                        stringMessage += ListItemPathsNotProcessed(LibAES.listFilePathErrorsRecursive);
                    }
                }
                return boolSuccess;
            }
            catch (Exception ex)
            {
                stringMessage = MessageOutputOnException("HomePage.EncryptFolderNoArchiveAsync()", ex);
                return boolSuccess;
            }
            finally
            {
                // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                stringMessageResult = string.Concat(stringMessage, string.Empty);
            }
        }

        /// <summary>
        /// Encrypt storageFileToEncrypt. Return true if encrypted, false otherwise.
        /// This is a wrapper method that selects proper encryption process to use depending on the location of storageFileToEncrypt.
        /// </summary>
        /// <param name="storageFolderParent">Parent Storagefolder of storageFileToEncrypt.</param>
        /// <param name="storageFileToEncrypt">storageFile to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFileAsync(StorageFolder storageFolderParent, StorageFile storageFileToEncrypt)
        {
            string stringFilename = LockerPathRemove(storageFileToEncrypt);
            // Try get the NoArchive folder.
            StorageFolder storageFolderNoArchive = null;
            IStorageItem iStorageItem = await mainPage.storageFolderLocker.TryGetItemAsync(mainPage.stringFoldernameNoArchive);
            if (iStorageItem != null)   // Item found but don't know if it was folder or file.
            {
                if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                    storageFolderNoArchive = (StorageFolder)iStorageItem;    // Found the NoArchive folder.
            }
            // Check if storageFileToEncrypt is in NoArchive folder.
            if (CheckHierarchy(storageFolderNoArchive, storageFileToEncrypt))
            {
                // Check if storageFileToEncrypt is encrypted. Abort if so. Special cases here, only check if NoArchive FileType
                // since EncryptFileNoArchiveAsync() will decrypt encypted files of wrong FileType before encrypting them.
                // Cannot check if file is encrypted until determine what folder it is in.
                if (!StorageFileIsEncrypted(storageFileToEncrypt, EnumEncryptedFileTypeCompareMode.NoArchive, null))    // ".aes", or ".enc".
                {
                    if (await EncryptFileNoArchiveAsync(storageFolderParent, storageFileToEncrypt))
                    {
                        // Encrypted storageFileToEncrypt using NoArchive methods. Success message saved in global variable stringMessageResult;
                        // Debug.WriteLine($"HomePage.EncryptFileAsync(): Encrypted storageFileToEncrypt.Name={storageFileToEncrypt.Name} using EncryptFileNoArchiveAsync() and returned true");
                        return true;
                    }
                }
                else
                {
                    // Special case, this message occurrs only if FileType is NoArchive.
                    // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                    List<string> list_OutputMsgError = new List<string>()
                    {
                        mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                        stringFilename,
                        mainPage.resourceLoader.GetString("HP_Error_Encrypt_FileIsEncrypted")   // since already encrypted
                    };
                    stringMessageResult = LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace);
                }
            }
            else
            {
                // Check if storageFileToEncrypt is encrypted. Abort if so. Special cases here, only check if Archive FileType
                // since EncryptFileArchiveAsync() will decrypt encypted files of wrong FileType before encrypting them.
                // Cannot check if file is encrypted until determine what folder it is in.
                if (!StorageFileIsEncrypted(storageFileToEncrypt, EnumEncryptedFileTypeCompareMode.Archive, null))    // ".arc", or ".zip".
                {
                    if (await EncryptFileArchiveAsync(storageFolderParent, storageFileToEncrypt))
                    {
                        // Encrypted storageFileToEncrypt using Archive methods. Success message saved in global variable stringMessageResult;
                        // Debug.WriteLine($"HomePage.EncryptFileAsync(): Encrypted storageFileToEncrypt.Name={storageFileToEncrypt.Name} using EncryptFileArchiveAsync() and returned true");
                        return true;
                    }
                }
                else
                {
                    // Special case, this message occurrs only if FileType is Archive.
                    // Copy stringMessage to global variable stringMessageResult since Async methods cannot use 'out' variables.
                    List<string> list_OutputMsgError = new List<string>()
                    {
                        mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                        stringFilename,
                        mainPage.resourceLoader.GetString("HP_Error_Encrypt_FileIsEncrypted")   // since already encrypted
                    };
                    stringMessageResult = LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace);
                }
            }
            // Could not encrypt storageFileToEncrypt. Error message saved in global variable stringMessageResult.
            // Debug.WriteLine($"HomePage.EncryptFileAsync(): Could not encrypt storageFileToEncrypt.Name={storageFileToEncrypt.Name} so returned false");
            return false;
        }

        /// <summary>
        /// Encrypt storageFolderToEncrypt. Return true if encrypted, false otherwise.
        /// This is a wrapper method that selects proper encryption process to use depending on location of storageFolderToEncrypt.
        /// </summary>
        /// <param name="storageFolderParent">Parent Storagefolder of storageFileToEncrypt.</param>
        /// <param name="storageFolderToEncrypt">storageFolder to encrypt.</param>
        /// <returns></returns>
        private async Task<bool> EncryptFolderAsync(StorageFolder storageFolderParent, StorageFolder storageFolderToEncrypt)
        {
            // Try get the NoArchive folder.
            StorageFolder storageFolderNoArchive = null;
            IStorageItem iStorageItem = await mainPage.storageFolderLocker.TryGetItemAsync(mainPage.stringFoldernameNoArchive);
            if (iStorageItem != null)   // Item found but don't know if it was folder or file.
            {
                if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                    storageFolderNoArchive = (StorageFolder)iStorageItem;    // Found the NoArchive folder.
            }
            // Check if storageFolderToEncrypt is in NoArchive folder.
            if (CheckHierarchy(storageFolderNoArchive, storageFolderToEncrypt))
            {
                // Encrypted files with proper FileType will be skipped. Encrypted files with 
                // improper FileType will be decrypted before encrypting with proper FileType.
                if (await EncryptFolderNoArchiveAsync(storageFolderToEncrypt))
                {
                    // Encrypted storageFolderToEncrypt using NoArchive methods. Success message saved in global variable stringMessageResult;
                    // Debug.WriteLine($"HomePage.EncryptFolderAsync(): Encrypted storageFolderToEncrypt.Name={storageFolderToEncrypt.Name} using EncryptFolderNoArchiveAsync() and returned true");
                    return true;
                }
            }
            else
            {
                // Encrypt folder in Locker folder which is not in NoArchive folder. This will archive folder into a file and then encrypt it.
                // This will decrypt encrypted files in storageFolderToEncrypt that do not have FileType that matches mainPage.stringExtensionArchive.
                if (await EncryptFolderArchiveAsync(storageFolderParent, storageFolderToEncrypt))
                {
                    // Encrypted storageFolderToEncrypt using Archive methods. Success message saved in global variable stringMessageResult;
                    // Debug.WriteLine($"HomePage.EncryptFolderAsync(): Encrypted storageFolderToEncrypt.Name={storageFolderToEncrypt.Name} using EncryptFolderArchiveAsync() and returned true");
                    return true;
                }
            }
            // Could not encrypt storageFolderToEncrypt. Error message saved in global variable stringMessageResult.
            // Debug.WriteLine($"HomePage.EncryptfolderAsync(): Could not encrypt storageFolderToEncrypt.Name={storageFolderToEncrypt.Name} so returned false");
            return false;
        }

        /// <summary>
        /// Decrypt storageFileToDecrypt. Return true if decrypted, false otherwise.
        /// This is a wrapper method that selects proper decryption process to use depending on FileType of storageFileToDecrypt.
        /// Calling method must ensure storageFileToDecrypt is encrypted.
        /// </summary>
        /// <param name="storageFolderParent">Parent Storagefolder of storageFileToDecrypt.</param>
        /// <param name="storageFileToDecrypt">storageFile to decrypt.</param>
        /// <returns></returns>
        private async Task<bool> DecryptFileAsync(StorageFolder storageFolderParent, StorageFile storageFileToDecrypt)
        {
            // Calling method must ensure storageFileToDecrypt is encrypted.
            string stringFileType = storageFileToDecrypt.FileType;
            // Check if storageFileToDecrypt has an NoArchive FileType. Continue if so.
            // StorageFileIsEncrypted(storageFileToDecrypt, EnumEncryptedFileTypeCompareMode.NoArchive, null) does not 
            // work here since only returns true for current mainPage.stringExtensionNoArchive FileType.
            if (stringFileType.Equals(mainPage.stringExtensionNoArchive, StringComparison.OrdinalIgnoreCase))   // ".aes"
            {
                if (await DecryptFileNoArchiveAsync(storageFolderParent, storageFileToDecrypt))
                {
                    // Decrypted storageFileToDecrypt. Success message saved in global variable stringMessageResult;
                    // Debug.WriteLine($"HomePage.DecryptFileAsync(): Decrypted storageFileToDecrypt.Name={storageFileToDecrypt.Name} using DecryptFileNoArchiveAsync() and returnd true");
                    return true;
                }
            }
            else
            {
                // storageFileToDecrypt has Archive FileType so continue without checking to verify.
                if (await DecryptFileArchiveAsync(storageFolderParent, storageFileToDecrypt))
                {
                    // Decrypted storageFileToDecrypt. Success message saved in global variable stringMessageResult;
                    // Debug.WriteLine($"HomePage.DecryptFileAsync(): Decrypted storageFileToDecrypt.Name={storageFileToDecrypt.Name} using DecryptFileArchiveAsync() and returned true");
                    return true;
                }
            }
            // Could not decrypt storageFileToDecrypt. Error message saved in global variable stringMessageResult.
            // Debug.WriteLine($"HomePage.DecryptFileAsync(): Could not decrypt storageFileToDecrypt.Name={storageFileToDecrypt.Name} so returned false");
            return false;
        }

        /// <summary>
        /// Recursive method that decrypts files found in storageFolderDecrypt. No parameter checking done.
        /// Method steps through each subfolder and attempts to decrypt any encrypted files found.
        /// Any decryption errors are added to listFileErrorsRecursive.
        /// 
        /// Calling method needs to set compare mode parameters enumEncryptedFileTypeCompareModeRecursive and 
        /// stringEncryptedFileTypeCompareRecursive used by StorageFileIsEncrypted() used in this method.
        /// Calling method also needs to set intFilesFoundRecursive=0, boolProcessSuccessRecursive=true, and clear listFileErrorsRecursive.
        /// </summary>
        /// <param name="storageFolderDecrypt">StorgeFolder to decrypt.</param>
        /// <returns></returns>
        private async Task DecryptFolderAsync(StorageFolder storageFolderDecrypt)
        {
            /****** Be extremely careful editing this method since it uses recursion. Method needs to run out to end without returns since recursive. *******/
            IReadOnlyList<StorageFile> listStorageFiles = await storageFolderDecrypt.GetFilesAsync();
            if (listStorageFiles != null)
            {
                if (listStorageFiles.Count > 0)
                {
                    foreach (StorageFile storageFileFound in listStorageFiles)
                    {
                        intFilesFoundRecursive++;   // This is set to 0 by calling method.
                        // Debug.WriteLine($"HomePage.DecryptFolderAsync(): Found {storageFileFound.Path}");
                        // Check if storageFileFound is encrypted. Continue if so. Parameters next line must be set by calling methods.
                        if (StorageFileIsEncrypted(storageFileFound, enumEncryptedFileTypeCompareModeRecursive, stringEncryptedFileTypeCompareRecursive))
                        {
                            // Debug.WriteLine($"HomePage.DecryptFolderAsync(): Attempting to decrypt {storageFileFound.Path}");
                            // If next line decrypts storageFileFound then keep going without pause and do not show User success message.
                            if (!await DecryptFileAsync(storageFolderDecrypt, storageFileFound))
                            {
                                boolProcessSuccessRecursive = false;                    // Error so set to false. This is set to true by calling method.
                                listFileErrorsRecursive.Add(storageFileFound.Path);     // Add file path to error list. This list is cleared by calling method.
                                // Debug.WriteLine($"HomePage.DecryptFolderAsync(): Could not decrypt {LockerPathRemove(storageFileFound)}");
                            }
                        }
                    }
                }
            }
            IReadOnlyList<StorageFolder> listStorageFolders = await storageFolderDecrypt.GetFoldersAsync();
            if (listStorageFolders != null)
            {
                if (listStorageFolders.Count > 0)
                {
                    foreach (StorageFolder storageFolderFound in listStorageFolders)
                    {
                        // Debug.WriteLine($"HomePage.DecryptFolderAsync(): Found Folder={storageFolderFound.Path}");
                        // Next line is recursive method call.
                        await DecryptFolderAsync(storageFolderFound);
                    }
                }
            }
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Set focus to 'ButHPViewLocker' when page loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            homePagePointer = this;     // Set value of HomePage pointer declared above.
            // Hide XAML layout rectangles by setting to same color as RelativePanel Background;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, true);
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButSettings, true);
            LibMPC.ButtonVisibility(ButPurchaseApp, false);
            LibMPC.ButtonVisibility(ButRateApp, false);
            ProgressBarHPShow(false);
            if (!mainPage.boolVerbose)
                TblkMsgSettings.Visibility = Visibility.Collapsed;   // Hide string if mainPage.boolVerbose=false.
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButLockerView,
                ButCreateFolderNoArchive,
                ButEncryptLocker,
                ButDecryptLocker,
                ButEncryptFile,
                ButDecryptFile,
                ButEncryptFolder,
                ButDecryptFolder,
                ButDeleteFile,
                ButDeleteFolder
            };
            // Add ButPurchaseApp to list if App has not been purchased.
            if (!mainPage.boolAppPurchased)
                listButtonsThisPage.Add(ButPurchaseApp);
            // Do not add ButRateApp to above list since content of button is usually longest item in list and causes some VisualStateGroup issues.
            LibMPC.SizePageButtons(listButtonsThisPage);
            PBarStatus.Foreground = LibMPC.colorError;      // Set color PBarStatus from default.
            ButRateApp.Foreground = LibMPC.colorSuccess;
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("HP_TblkPageTitle"));
            LibMPC.OutputMsgBright(TblkMsgSettings, mainPage.resourceLoader.GetString("HP_TblkMsgSettings"));
            // Change default settings of strings used in LibMPC to show translated values.
            LibMPC.stringAppPurchaseTrueMsg1 = mainPage.resourceLoader.GetString("LibMPC_StringAppPurchaseTrueMsg1");
            LibMPC.stringAppPurchaseFalseMsg1 = mainPage.resourceLoader.GetString("LibMPC_StringAppPurchaseFalseMsg1");
            LibMPC.stringAppPurchaseFalseMsg2 = mainPage.resourceLoader.GetString("LibMPC_StringAppPurchaseFalseMsg2");
            LibMPC.stringAppPurchaseFalseMsg3 = mainPage.resourceLoader.GetString("LibMPC_StringAppPurchaseFalseMsg3");
            ReadDataStoreHPValues();
            UpdateToggleStatus();
            await MessageHomePageLoadAsync();
            await ButCreateFolderNoArchiveUpdateAsync();      // Enable or disable ButCreateFolderNoArchive button.
            await AppPurchaseCheck();
            AppRatedCheck();
            // Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            ButLockerView.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Launch MS FileExplorer to Locker folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButLockerView_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                await LibMPC.LaunchFileExplorerAsync(mainPage.storageFolderLocker);
                // File explorer opened to {0}  <-- Note: Translate whole phrase!
                LibMPC.OutputMsgSuccess(TblkResult, string.Format(mainPage.resourceLoader.GetString("HP_Success_FileExplorer"), mainPage.storageFolderLocker.Path));
                await ButCreateFolderNoArchiveUpdateAsync();    // Enable or disable ButCreateFolderNoArchive button.
            }
            else
            {
                await EnablePageItems(false);     // Could not find locker so disable all butttons preventing user from doing anything else.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Find or create NoArchive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButCreateFolderNoArchive_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (await mainPage.FolderCheckLockerAsync())     // Check if Locker folder still exists.
            {
                // Check if NoArchive folder exists.  Create if not.
                StorageFolder storageFolderNoArchive = await mainPage.FolderCheckNoArchiveAsync();
                if (storageFolderNoArchive != null)
                {
                    LibMPC.OutputMsgSuccess(TblkResult, mainPage.stringFolderCheckOutput);     // Found or created folder 'mainPage.stringFoldernameNoArchive'
                    LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, false);
                }
                else
                {
                    LibMPC.OutputMsgError(TblkResult, mainPage.stringFolderCheckOutput);       // Could not find or create folder 'mainPage.stringFoldernameNoArchive'
                    LibMPC.ButtonIsEnabled(ButCreateFolderNoArchive, true);
                }
            }
            else
            {
                await EnablePageItems(false);     // Could not find Locker folder so disable all butttons preventing user from doing anything else.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Secure Locker folder by encrypting any unencrypted items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButEncryptLocker_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())    // Check if Locker folder still exists.
            {
                // App cannot check if Locker folder is locked since does not have permission to rename Locker folder.
                // Therefore, need to process each item found in Locker folder individually and check lock status on it before processing.
                string stringMessage = string.Empty;
                bool boolError = false;     // Set to true if any errors.
                ProgressBarHPShow(true);
                // Get list of files in Locker folder.
                IReadOnlyList<StorageFile> listStorageFilesInLockerFolder = await mainPage.storageFolderLocker.GetFilesAsync();
                if (listStorageFilesInLockerFolder != null)
                {
                    foreach (StorageFile storageFileFound in listStorageFilesInLockerFolder)
                    {
                        // Check if storageFileFound is encrypted. Abort if so. Special case here, only check if Archive FileType
                        // since EncryptFileArchiveAsync() will decrypt encypted files of wrong FileType before encrypting them.
                        if (!StorageFileIsEncrypted(storageFileFound, EnumEncryptedFileTypeCompareMode.Archive, null))  // ".arc" or ".zip".
                        {
                            // Check if storageFileFound is locked. Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFileFound))
                            {
                                // Success thus far so try to encrypt storageFileFound.
                                ItemIsBeingProcessedMessage(storageFileFound);
                                // If next line encrypts storageFileFound then keep going without pause and do not show User success message.
                                if (!await EncryptFileArchiveAsync(mainPage.storageFolderLocker, storageFileFound))
                                {
                                    // Could not encrypt storageFileFound so abort and show User error message.
                                    boolError = true;
                                    stringMessage = stringMessageResult;
                                    break;     // Could not encrypt storageFileFound so break from foreach loop.
                                }
                            }
                            else
                            {
                                boolError = true;
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                    LockerPathRemove(storageFileFound),
                                    mainPage.resourceLoader.GetString("HP_Error_FileLocked")    // since file is locked.  Another application may be using file.
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                                break;     // Could not encrypt storageFileFound so break from foreach loop.
                            }
                        }
                    }
                }
                if (!boolError)     // If no errors then continue...
                {
                    // Get list of folders in Locker folder.
                    IReadOnlyList<StorageFolder> listStorageFoldersInLockerFolder = await mainPage.storageFolderLocker.GetFoldersAsync();
                    if (listStorageFoldersInLockerFolder != null)
                    {
                        foreach (StorageFolder storageFolderFound in listStorageFoldersInLockerFolder)
                        {
                            // Check if storageFolderFound is locked. Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFolderFound))
                            {
                                // Success thus far so try to encrypt storageFolderFound.
                                ItemIsBeingProcessedMessage(storageFolderFound);
                                if (storageFolderFound.Name.Equals(mainPage.stringFoldernameNoArchive, StringComparison.OrdinalIgnoreCase))     // Check if NoArchive folder.
                                {
                                    // Encrypt NoArchive folder in one big bite. This may take awhile and individual files will not be shown as they are encrypted.
                                    // Encrypted files with proper FileType will be skipped. Encrypted files with improper FileType will be decrypted before encrypting
                                    // with proper FileType.
                                    // If next line encrypts storageFolderFound then keep going without pause and do not show User success message.
                                    if (!await EncryptFolderNoArchiveAsync(storageFolderFound))
                                    {
                                        // Could not encrypt all files in storageFolderFound, which is NoArchive folder, so abort and show User error message.
                                        boolError = true;
                                        stringMessage = stringMessageResult;
                                        break;     // Could not encrypt storageFolderFound so break from foreach loop.
                                    }
                                }
                                else
                                {
                                    // Encrypt folder in Locker folder which is not in NoArchive folder. This will archive folder into a file and then encrypt it.
                                    // This will decrypt encrypted files in storageFolderFound that do not have FileType that matches mainPage.stringExtensionArchive.
                                    // If next line encrypts storageFolderFound then keep going without pause and do not show User success message.
                                    if (!await EncryptFolderArchiveAsync(mainPage.storageFolderLocker, storageFolderFound))
                                    {
                                        // Could not encrypt storageFolderFound so abort and show User error message.
                                        boolError = true;
                                        stringMessage = stringMessageResult;
                                        break;     // Could not encrypt storageFolderFound so break from foreach loop.
                                    }
                                }
                            }
                            else
                            {
                                boolError = true;
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                    LockerPathRemove(storageFolderFound),
                                    mainPage.resourceLoader.GetString("HP_Error_FolderLocked")  // since folder is locked.  Another application may be using a file in the folder.
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                            }
                        }
                    }
                }
                ProgressBarHPShow(false);
                await EnablePageItems(true);
                if (boolError)
                {
                    // Error occurred encrypting Locker folder so show User error message.
                    LibMPC.OutputMsgError(TblkResult, stringMessage);
                }
                else
                {
                    // Encrypted Locker folder successfully so show User success message.
                    List<string> list_OutputMsgSuccess = new List<string>()
                    {
                        mainPage.resourceLoader.GetString("HP_Success_Encrypt"),    // Encrypted
                        LockerPathRemove(mainPage.storageFolderLocker),
                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                    };
                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                    if (boolExitApp)
                        Application.Current.Exit();     // Exit App since boolExitApp set to true in Settings page.
                }
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Attempt to decrypt all encrypted files in Locker folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDecryptLocker_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())    // Check if Locker folder still exists.
            {
                // App cannot check if Locker folder is locked since does not have permission to rename Locker folder.
                // Therefore, need to process each item found in Locker folder individually and check lock status on it before processing.
                string stringMessage = string.Empty;
                bool boolError = false;     // Set to true if any errors.
                ProgressBarHPShow(true);
                // App can not check if Locker folder is locked since does not have permission to rename Locker folder.
                // Therefore, need to process each item found in Locker folder and check lock status on it before proceeding.
                //
                // Get list of files in Locker folder.
                IReadOnlyList<StorageFile> listStorageFilesInLockerFolder = await mainPage.storageFolderLocker.GetFilesAsync();
                if (listStorageFilesInLockerFolder != null)
                {
                    foreach (StorageFile storageFileFound in listStorageFilesInLockerFolder)
                    {
                        // Check if storageFileFound is encrypted. continue if so.
                        if (StorageFileIsEncrypted(storageFileFound, EnumEncryptedFileTypeCompareMode.All, null))
                        {
                            // Check if storageFileFound is locked. Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFileFound))
                            {
                                // Success thus far so try to decrypt storageFileFound.
                                ItemIsBeingProcessedMessage(storageFileFound);
                                // If next line decrypts storageFileFound then keep going without pause and do not show User success message.
                                if (!await DecryptFileAsync(mainPage.storageFolderLocker, storageFileFound))
                                {
                                    // Could not decrypt storageFileFound so abort and show User error message.
                                    boolError = true;
                                    stringMessage = stringMessageResult;
                                    break;     // Could not decrypt storageFileFound so break from foreach loop.
                                }
                            }
                            else
                            {
                                boolError = true;
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Decrypt"),      // Could not decrypt
                                    LockerPathRemove(storageFileFound),
                                    mainPage.resourceLoader.GetString("HP_Error_FileLocked")    // since file is locked.  Another application may be using file.
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                                break;     // Could not decrypt storageFileFound so break from foreach loop.
                            }
                        }
                    }
                }
                if (!boolError)     // If no errors then continue...
                {
                    // Get list of folders in Locker folder.
                    IReadOnlyList<StorageFolder> listStorageFoldersInLockerFolder = await mainPage.storageFolderLocker.GetFoldersAsync();
                    if (listStorageFoldersInLockerFolder != null)
                    {
                        foreach (StorageFolder storageFolderFound in listStorageFoldersInLockerFolder)
                        {
                            string stringFoldername = LockerPathRemove(storageFolderFound);     // Save truncated path foldername for output.
                            // Check if storageFolderFound is locked. Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFolderFound))
                            {
                                // Success thus far so try to decrypt storageFolderFound.
                                ItemIsBeingProcessedMessage(storageFolderFound);
                                // Set compare mode to use in DecryptFolderAsync() called below to decrypt all files.
                                enumEncryptedFileTypeCompareModeRecursive = EnumEncryptedFileTypeCompareMode.All;
                                stringEncryptedFileTypeCompareRecursive = null;     // Set to null since not used when decrypt all files.
                                intFilesFoundRecursive = 0;             // Zero counter.
                                boolProcessSuccessRecursive = true;     // Remains true if no decryption errors.
                                listFileErrorsRecursive.Clear();        // Reset/Clear list of file path errors from previous runs.
                                await DecryptFolderAsync(storageFolderFound);   // Recursive method that decrypts any Archive or NoArchive files in subfolders.
                                if (!boolProcessSuccessRecursive)
                                {
                                    // Could not decypt at least one file in storageFolderFound.
                                    boolError = true;
                                    // Decrypted {0} files in {1}  <-- Note: Translate whole phrase!
                                    stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FolderFilesFailed"), intFilesFoundRecursive - listFileErrorsRecursive.Count, stringFoldername);
                                    stringMessage += ListItemPathsNotProcessed(listFileErrorsRecursive);    // items not processed
                                    break;     // Could not decrypt storageFolderFound so break from foreach loop.
                                }
                            }
                            else
                            {
                                boolError = true;
                                List<string> list_stringMessage = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Decrypt"),      // Could not decrypt
                                    stringFoldername,
                                    mainPage.resourceLoader.GetString("HP_Error_FolderLocked")  // since folder is locked.  Another application may be using a file in the folder.
                                };
                                stringMessage = LibMPC.JoinListString(list_stringMessage, EnumStringSeparator.OneSpace);
                            }
                        }
                    }
                }
                ProgressBarHPShow(false);
                await EnablePageItems(true);
                if (boolError)
                {
                    // Error occurred decrypting Locker folder so show User error message.
                    LibMPC.OutputMsgError(TblkResult, stringMessage);
                    // Debug.WriteLine($"HomePage.ButDecryptLocker_Click(): Did NOT decrypt Locker folder successfully.");
                }
                else
                {
                    // Decrypted Locker folder successfully so show User success message.
                    List<string> list_OutputMsgSuccess = new List<string>()
                    {
                        mainPage.resourceLoader.GetString("HP_Success_Decrypt"),    // Decrypted
                        LockerPathRemove(mainPage.storageFolderLocker),
                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                    };
                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                    // Debug.WriteLine($"HomePage.ButDecryptLocker_Click(): Decrypted Locker folder successfully.");
                }
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick a file anywhere in Locker folder to encrypt. File encrytion method used depends on location of file.
        /// If file is in NoArchive folder then encrypt it using NoArchive methods. Otherwise encrypt it using Archive methods.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButEncryptFile_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFile_Encrypt"));     // Pick file to encrypt
                FileOpenPicker fileOpenPicker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                // Need at least one filter to prevent exception.
                fileOpenPicker.FileTypeFilter.Add("*");
                StorageFile storageFilePicked = await fileOpenPicker.PickSingleFileAsync();
                if (storageFilePicked != null)
                {
                    // Check if storageFilePicked is in Locker folder. Continue if so.
                    if (CheckHierarchy(mainPage.storageFolderLocker, storageFilePicked))
                    {
                        string stringFilename = LockerPathRemove(storageFilePicked);    // Save truncated path filename for output.
                        // Check if storageFilePicked has read and write access to parent folder. Continue if so.
                        StorageFolder storageFolderParent = await storageFilePicked.GetParentAsync();
                        if (storageFolderParent != null)
                        {
                            // Check if storageFilePicked is locked. Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFilePicked))
                            {
                                // Success thus far so try to encrypt storageFilePicked.
                                ItemIsBeingProcessedMessage(storageFilePicked);
                                ProgressBarHPShow(true);
                                if (await EncryptFileAsync(storageFolderParent, storageFilePicked))
                                {
                                    // Encrypted storageFilePicked successfully so show User success message.
                                    ProgressBarHPShow(false);
                                    List<string> list_OutputMsgSuccess = new List<string>()
                                    {
                                        stringMessageResult,    // Encrypted 'LockerPathRemove(storageFilePicked)'
                                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                    };
                                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                                }
                                else
                                {
                                    // Could not encrypt storageFilePicked so show User error message.
                                    ProgressBarHPShow(false);
                                    LibMPC.OutputMsgError(TblkResult, stringMessageResult);
                                }
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                    stringFilename,
                                    mainPage.resourceLoader.GetString("HP_Error_FileLocked")    // since file is locked.  Another application may be using file.
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            List<string> list_OutputMsgError = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Encrypt"),         // Could not encrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_ParentFolder")     // since could not get read and write access to parent folder.
                            };
                            LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Encrypt"),             // Could not encrypt
                            storageFilePicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else    // User did not select a file.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));  // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick encypted file anywhere in Locker folder to decrypt. File decrytion method used depends on Filetype of picked file.
        /// If Filetype is NoArchive then decrypt it using NoArchive methods. Otherwise decrypt it using Archive methods.
        /// Original item could be a folder or file. Decrypt content back to original item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDecryptFile_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())    // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFile_Decrypt"));     // Pick file to decrypt
                FileOpenPicker fileOpenPicker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                fileOpenPicker.FileTypeFilter.Add(mainPage.stringExtensionArchive);
                fileOpenPicker.FileTypeFilter.Add(mainPage.stringExtensionNoArchive);
                StorageFile storageFilePicked = await fileOpenPicker.PickSingleFileAsync();
                if (storageFilePicked != null)
                {
                    // Check if storageFilePicked is in Locker folder. Continue if so.
                    if (CheckHierarchy(mainPage.storageFolderLocker, storageFilePicked))
                    {
                        string stringFilename = LockerPathRemove(storageFilePicked);    // Save truncated path filename for output. 
                        // Check if storageFilePicked has read and write access to parent folder. Continue if so.
                        StorageFolder storageFolderParent = await storageFilePicked.GetParentAsync();
                        if (storageFolderParent != null)
                        {
                            // Do not check if storageFilePicked is encypted since file picker only allows selection of encrypted FileTypes.
                            // Check if storageFilePicked is locked.  Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFilePicked))
                            {
                                // Success thus far so try to decrypt storageFilePicked.
                                ItemIsBeingProcessedMessage(storageFilePicked);
                                ProgressBarHPShow(true);
                                if (await DecryptFileAsync(storageFolderParent, storageFilePicked))
                                {
                                    // Decrypted storageFilePicked successfully so show User success message.
                                    ProgressBarHPShow(false);
                                    List<string> list_OutputMsgSuccess = new List<string>()
                                    {
                                        stringMessageResult,    // Decrypted 'LockerPathRemove(storageFilePicked)'
                                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                    };
                                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                                }
                                else
                                {
                                    // Could not decrypt storageFilePicked so show User error message.
                                    ProgressBarHPShow(false);
                                    LibMPC.OutputMsgError(TblkResult, stringMessageResult);
                                }
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Decrypt"),      // Could not decrypt
                                    stringFilename,
                                    mainPage.resourceLoader.GetString("HP_Error_FileLocked")    // since file is locked.  Another application may be using file.
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            List<string> list_OutputMsgError = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt"),         // Could not decrypt
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_ParentFolder")     // since could not get read and write access to parent folder
                            };
                            LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt"),             // Could not decrypt
                            storageFilePicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else   // User did not select a file.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));  // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick folder anywhere in Locker folder to encrypt. Encryption process used on files found in folder depends on location of file.
        /// If file is in NoArchive folder then encrypt it using NoArchive methods. Otherwise encrypt it using Archive methods.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButEncryptFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFolder_Encrypt"));   // Pick folder to encrypt
                FolderPicker folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                // Need at least one filter to prevent exception.
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder storageFolderPicked = await folderPicker.PickSingleFolderAsync();
                if (storageFolderPicked != null)
                {
                    // Check if storageFolderSelected is in Locker folder. Continue if so.
                    StorageFolder storageFolderSelected = await StorageFolderFromFolderPickerAsync(storageFolderPicked);
                    if (storageFolderSelected != null)
                    {
                        string stringFoldername = LockerPathRemove(storageFolderSelected);    // Save truncated path foldername for output.
                        // Check if storageFolderSelected has read and write access to parent folder. Continue if so.
                        StorageFolder storageFolderParent = await storageFolderSelected.GetParentAsync();
                        if (storageFolderParent != null)
                        {
                            // Check if storageFolderSelected is locked.  Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFolderSelected))
                            {
                                // Success thus far so try to encrypt storageFolderSelected.
                                ItemIsBeingProcessedMessage(storageFolderSelected);
                                ProgressBarHPShow(true);
                                if (await EncryptFolderAsync(storageFolderParent, storageFolderSelected))
                                {
                                    // Encrypted storageFolderSelected successfully so show User success message.
                                    ProgressBarHPShow(false);
                                    List<string> list_OutputMsgSuccess = new List<string>()
                                    {
                                        stringMessageResult,        // Encrypted 'LockerPathRemove(storageFolderSelected)'
                                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                    };
                                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                                }
                                else
                                {
                                    // Could not encrypt storageFolderSelected so show User error message.
                                    ProgressBarHPShow(false);
                                    LibMPC.OutputMsgError(TblkResult, stringMessageResult);
                                }
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),      // Could not encrypt
                                    stringFoldername,
                                    mainPage.resourceLoader.GetString("HP_Error_FolderLocked")  // since folder is locked.  Another application may be using a file in the folder.
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Encrypt"),         // Could not encrypt
                                    stringFoldername,
                                    mainPage.resourceLoader.GetString("HP_Error_ParentFolder")     // since could not get read and write access to parent folder.
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Encrypt"),             // Could not encrypt
                            storageFolderPicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else    // User did not select a folder.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));  // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick folder anywhere in Locker folder to decrypt. Decryption process used on files found in folder depends on Filetype of file.
        /// If Filetype is NoArchive then decrypt it using NoArchive methods. Otherwise decrypt it using Archive methods.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDecryptFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFolder_Decrypt"));       // Pick folder to decrypt
                FolderPicker folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                // Need at least one filter to prevent exception.
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder storageFolderPicked = await folderPicker.PickSingleFolderAsync();
                if (storageFolderPicked != null)
                {
                    // Check if storageFolderPicked is in Locker folder. Continue if so.
                    StorageFolder storageFolderSelected = await StorageFolderFromFolderPickerAsync(storageFolderPicked);
                    if (storageFolderSelected != null)
                    {
                        string stringFoldername = LockerPathRemove(storageFolderSelected);     // Save truncated path foldername for output.
                        // Check if storageFolderSelected has read and write access to parent folder. Continue if so.
                        StorageFolder storageFolderParent = await storageFolderSelected.GetParentAsync();
                        if (storageFolderParent != null)
                        {
                            // Check if storageFolderSelected is locked.  Abort if so.
                            if (!await LibAES.IStorageItemLockCheckAsync(storageFolderSelected))
                            {
                                // Success thus far so try to decrypt storageFolderSelected.
                                ItemIsBeingProcessedMessage(storageFolderSelected);
                                ProgressBarHPShow(true);
                                // Set compare mode to use in DecryptFolderAsync() called below to decrypt all files.
                                enumEncryptedFileTypeCompareModeRecursive = EnumEncryptedFileTypeCompareMode.All;
                                stringEncryptedFileTypeCompareRecursive = null;     // Set to null since not used when decrypt all files.
                                intFilesFoundRecursive = 0;             // Zero counter.
                                boolProcessSuccessRecursive = true;     // Remains true if no decryption errors.
                                listFileErrorsRecursive.Clear();        // Reset/Clear list of file path errors from previous runs.
                                await DecryptFolderAsync(storageFolderSelected);    // Recursive method that decrypts any Archive or NoArchive files in subfolders.
                                ProgressBarHPShow(false);
                                if (boolProcessSuccessRecursive)
                                {
                                    // Decrypted storageFolderSelected successfully so show User success message.
                                    List<string> list_OutputMsgSuccess = new List<string>()
                                    {
                                        mainPage.resourceLoader.GetString("HP_Success_Decrypt"),    // Decrypted
                                        stringFoldername,
                                        string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                    };
                                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                                }
                                else
                                {
                                    // Could not decypt at least one file in storageFolderSelected.
                                    // Decrypted {0} files in {1}  <-- Note: Translate whole phrase!
                                    string stringMessage = string.Format(mainPage.resourceLoader.GetString("HP_Error_Decrypt_FolderFilesFailed"), intFilesFoundRecursive - listFileErrorsRecursive.Count, stringFoldername);
                                    stringMessage += ListItemPathsNotProcessed(listFileErrorsRecursive);    // items not processed
                                    LibMPC.OutputMsgError(TblkResult, stringMessage);
                                }
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_Decrypt"),      // Could not decrypt
                                    stringFoldername,
                                    mainPage.resourceLoader.GetString("HP_Error_FolderLocked")  // since folder is locked.  Another application may be using a file in the folder.
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            List<string> list_OutputMsgError = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_Decrypt"),          // Could not decrypt
                                stringFoldername,
                                mainPage.resourceLoader.GetString("HP_Error_ParentFolder")      // since could not get read and write access to parent folder.
                            };
                            LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_Decrypt"),             // Could not decrypt
                            storageFolderPicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else    // User did not select a file.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));  // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick file to secure delete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFile_Delete"));      // Pick file to secure delete
                FileOpenPicker fileOpenPicker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                // Need at least one filter to prevent exception.
                fileOpenPicker.FileTypeFilter.Add("*");
                StorageFile storageFilePicked = await fileOpenPicker.PickSingleFileAsync();
                if (storageFilePicked != null)
                {
                    // Check if storageFilePicked is in Locker folder. Continue if so.
                    if (CheckHierarchy(mainPage.storageFolderLocker, storageFilePicked))
                    {
                        string stringFilename = LockerPathRemove(storageFilePicked);    // Save truncated path filename for output.
                        // Check if storageFilePicked is locked.  Abort if so.
                        if (!await LibAES.IStorageItemLockCheckAsync(storageFilePicked))
                        {
                            ItemIsBeingProcessedMessage(storageFilePicked);
                            ProgressBarHPShow(true);
                            // storageFilePicked lock check done here so set parameter boolCheckIfLocked to false so lock check not done again.
                            bool boolSuccess = await LibAES.StorageFileDeleteSecureAsync(storageFilePicked, false);
                            ProgressBarHPShow(false);
                            if (boolSuccess)
                            {
                                List<string> list_OutputMsgSuccess = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Success_DeleteSecure"),   // Secure deleted
                                    stringFilename,
                                    string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                };
                                LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),                 // Could not secure delete
                                    stringFilename,
                                    mainPage.resourceLoader.GetString("HP_Error_DeleteSecure_ProcessFail"),     // since secure delete process failed
                                    string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            // Debug.WriteLine($"HomePage.ButDeleteSecureFile_Click: {storageFilePicked.Name} is locked.");
                            List<string> list_OutputMsgError = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),     // Could not secure delete
                                stringFilename,
                                mainPage.resourceLoader.GetString("HP_Error_FileLocked")        // since file is locked.  Another application may be using file.
                            };
                            LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),        // Could not secure delete
                            storageFilePicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else   // User did not select a file.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));  // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Pick folder to secure delete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await EnablePageItems(false);
            if (await mainPage.FolderCheckLockerAsync())     // Check if locker folder still exists.
            {
                LibMPC.OutputMsgNormal(TblkResult, mainPage.resourceLoader.GetString("HP_PickFolder_Delete"));    // Pick folder to secure delete
                FolderPicker folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                // Need at least one filter to prevent exception.
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder storageFolderPicked = await folderPicker.PickSingleFolderAsync();
                if (storageFolderPicked != null)
                {
                    // Check if storageFolderPicked is in Locker folder. Continue if so.
                    StorageFolder storageFolderSelected = await StorageFolderFromFolderPickerAsync(storageFolderPicked);
                    if (storageFolderSelected != null)
                    {
                        string stringFoldername = LockerPathRemove(storageFolderSelected);    // Save truncated path foldername for output since folder will be deleted.
                        // Check if storageFolderSelected is locked.  Abort if so.
                        if (!await LibAES.IStorageItemLockCheckAsync(storageFolderSelected))
                        {
                            ItemIsBeingProcessedMessage(storageFolderSelected);
                            ProgressBarHPShow(true);
                            // storageFolderSelected lock check done here so set parameter boolCheckIfLocked to false so lock check not done again.
                            bool boolSuccess = await LibAES.StorageFolderDeleteSecureAsync(storageFolderSelected, false);
                            ProgressBarHPShow(false);
                            if (boolSuccess)
                            {
                                List<string> list_OutputMsgSuccess = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Success_DeleteSecure"),   // Secure deleted
                                    stringFoldername,
                                    string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                };
                                LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_OutputMsgSuccess, EnumStringSeparator.OneSpace));
                            }
                            else
                            {
                                List<string> list_OutputMsgError = new List<string>()
                                {
                                    mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),                 // Could not secure delete
                                    stringFoldername,
                                    mainPage.resourceLoader.GetString("HP_Error_DeleteSecure_ProcessFail"),     // since secure delete process failed
                                    string.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)
                                };
                                LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                            }
                        }
                        else
                        {
                            List<string> list_OutputMsgError = new List<string>()
                            {
                                mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),     // Could not secure delete
                                stringFoldername,
                                mainPage.resourceLoader.GetString("HP_Error_FolderLocked")      // since folder is locked.  Another application may be using a file in the folder.
                            };
                            LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                        }
                    }
                    else
                    {
                        List<string> list_OutputMsgError = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("HP_Error_DeleteSecure"),        // Could not secure delete
                            storageFolderPicked.Path,
                            mainPage.resourceLoader.GetString("HP_Error_HierarchyOutside"),    // since located outside of folder
                            mainPage.storageFolderLocker.Name
                        };
                        LibMPC.OutputMsgError(TblkResult, LibMPC.JoinListString(list_OutputMsgError, EnumStringSeparator.OneSpace));
                    }
                }
                else    // User did not select a folder.
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));      // Did not pick anything
                await EnablePageItems(true);
            }
            else
            {
                // Could not find Locker folder. All buttons remain disabled.
                // Could not find locker.  Did you rename or move or delete locker while application was open?  Exit application and start again to reset locker.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("HP_Error_LockerNotFound"));
            }
        }

        /// <summary>
        /// Purchase application button. Button visible if application has not been purchased, collapsed otherwise.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButPurchaseApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await AppPurchaseBuy();
        }

        /// <summary>
        /// Invoked when user clicks hyperlink button ButRateApp. MS Store popup box will lock out all access to App.
        /// Therefore no reason to hide buttons on page. Do not show ButRateApp link again after User rates App.
        /// Goal is to get more App ratings in Microsoft Store without hassling User too much.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButRateApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (await mainPage.RateAppInW10Store())
                LibMPC.ButtonVisibility(ButRateApp, false);
        }

    }
}
