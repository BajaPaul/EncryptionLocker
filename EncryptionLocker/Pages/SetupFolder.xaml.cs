using EncryptionLocker.Common;
using LibraryCoder.AesEncryption;
using LibraryCoder.MainPageCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EncryptionLocker.Pages
{
    public sealed partial class SetupFolder : Page
    {
        /// <summary>
        /// Pointer to MainPage is needed to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        /// <summary>
        /// After install and on very first of App, check the create samples CheckBox.
        /// Then uncheck it on continue. Then use saved settting after that.
        /// </summary>
        private bool boolFirstRun;

        /// <summary>
        /// For this page, use 'SF_' prefix, shorthand for SetupFolder, for variable names in Resource.resw file to keep them together.
        /// </summary>
        public SetupFolder()
        {
            InitializeComponent();
            // Load language resource values for buttons on page before Page_Loaded() event so buttons render properly.
            ButFolderPicker.Content = mainPage.resourceLoader.GetString("SF_ButFolderPicker");
            ButContinue.Content = mainPage.resourceLoader.GetString("UMP_ButContinue");
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Read or create data store values ds_BoolFirstRun and ds_BoolCboxSamples. Check sample CheckBox on first run and then use saved value after that.
        /// </summary>
        private void DataStoreFirstRun()
        {
            if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_BoolFirstRun))
            {
                // Bool value of ds_BoolFirstRun not used. Only existance of it is checked.
                if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_BoolCboxSamples))
                {
                    // Set CheckBox to same value User left it at last time.
                    object objectBoolCboxSamples = mainPage.applicationDataContainer.Values[mainPage.ds_BoolCboxSamples];
                    if (objectBoolCboxSamples.Equals(false))
                        CboxSamples.IsChecked = false;      // Uncheck the checkBox.
                    else
                        CboxSamples.IsChecked = true;       // Check the checkBox.
                }
                boolFirstRun = false;   // Always set to false after first run of App.
            }
            else    // Did not find data store value ds_BoolFirstRun so this is first run of App.
            {
                boolFirstRun = true;
                CboxSamples.IsChecked = true;   // Check the checkBox.
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolFirstRun] = false;     // Write setting to data store.
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolCboxSamples] = true;   // Write setting to data store.
            }
        }

        /// <summary>
        /// Create a sample folder. Return folder if found or created. Skip if encrypted version of folder exists.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder.</param>
        /// <param name="stringFoldername">Foldername of sample folder.</param>
        /// <returns></returns>
        private async Task<StorageFolder> CreateSampleFolderAsync(StorageFolder storageFolderParent, string stringFoldername)
        {
            try
            {
                IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(stringFoldername);
                if (iStorageItem != null)
                {
                    if (iStorageItem.IsOfType(StorageItemTypes.Folder))
                        return (StorageFolder)iStorageItem;   // Folder exist.
                }
                iStorageItem = await storageFolderParent.TryGetItemAsync($"{stringFoldername}{mainPage.stringExtensionArchive}");
                if (iStorageItem != null)
                    return null;    // Encrypted version of folder exists with FileType ".arc".
                // Encrypted version of folder does not exist so create sample folder.
                StorageFolder storageFolderSample = await storageFolderParent.CreateFolderAsync(stringFoldername);
                if (storageFolderSample != null)
                {
                    // Debug.WriteLine($"SetupFolder.CreateSampleFolderAsync(): Created folder {storageFolderSample.Path}");
                    mainPage.boolSamplesCreated = true;
                    return storageFolderSample;
                }
                return null;
            }
            catch (Exception ex)
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("SetupFolder.CreateSampleFolderAsync()", ex.GetType()));
                return null;
                throw;
            }
        }

        /// <summary>
        /// Create sample file. Return file if found or created. Skip if encrypted version of file exists.
        /// </summary>
        /// <param name="storageFolderParent">Parent folder.</param>
        /// <param name="stringFilename">Filename of sample file including extension.</param>
        /// <param name="stringFileContent">Content to place in sample file.</param>
        /// <returns></returns>
        private async Task<StorageFile> CreateSampleFileAsync(StorageFolder storageFolderParent, string stringFilename, string stringFileContent)
        {
            try
            {
                IStorageItem iStorageItem = await storageFolderParent.TryGetItemAsync(stringFilename);
                if (iStorageItem != null)
                {
                    if (iStorageItem.IsOfType(StorageItemTypes.File))
                        return (StorageFile)iStorageItem;   // File exist.
                }
                iStorageItem = await storageFolderParent.TryGetItemAsync($"{stringFilename}{mainPage.stringExtensionNoArchive}");
                if (iStorageItem != null)
                    return null;   // Encrypted version of file exists with FileType ".aes".
                iStorageItem = await storageFolderParent.TryGetItemAsync($"{stringFilename}{mainPage.stringExtensionArchive}");
                if (iStorageItem != null)
                    return null;   // Encrypted version of file exists with FileType ".arc".
                // Encrypted version of file does not exist so create sample file.
                StorageFile storageFileSample = await storageFolderParent.CreateFileAsync(stringFilename);
                if (storageFileSample != null)
                {
                    // Write content to file.
                    IBuffer iBufferFileContent = LibAES.IBufferFromString($"{stringFilename}{Environment.NewLine}{Environment.NewLine}{stringFileContent}{Environment.NewLine}");
                    if (iBufferFileContent != null)
                    {
                        await FileIO.WriteBufferAsync(storageFileSample, iBufferFileContent);
                        mainPage.boolSamplesCreated = true;
                        // Debug.WriteLine($"SetupFolder.CreateSampleFolderAsync(): Created file {storageFileSample.Path}");
                        return storageFileSample;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("SetupFolder.CreateSampleFileAsync()", ex.GetType()));
                return null;
                throw;
            }
        }

        /// <summary>
        /// Create sample files and folders in Locker folder.
        /// </summary>
        /// <returns></returns>
        private async Task CreateSamplesItemsAsync()
        {
            try
            {
                mainPage.boolSamplesCreated = false;    // Set to false until a sample item is created since sample item may already exist.
                string stringFilename1 = mainPage.resourceLoader.GetString("SF_SampleFilename1");      // Instructions and tips
                string stringFilename2 = mainPage.resourceLoader.GetString("SF_SampleFilename2");      // Sample file
                string stringFoldername = mainPage.resourceLoader.GetString("SF_SampleFoldername");    // Sample folder
                // Build content string of sample files.
                string stringSampleFileContent = LibMPC.JoinListString(Translate.TRS_SF_List_SampleFile_Content, EnumStringSeparator.TwoNewlines);   // Do not assemble string until needed to save memory.
                string stringSampleName;
                StorageFolder storageFolderResult;
                // Create sample files and folders in Locker folder.
                stringSampleName = $"{stringFilename1}{ mainPage.stringExtensionTxt}";
                await CreateSampleFileAsync(mainPage.storageFolderLocker, stringSampleName, stringSampleFileContent);
                stringSampleName = $"{stringFilename2} 01{mainPage.stringExtensionTxt}";
                await CreateSampleFileAsync(mainPage.storageFolderLocker, stringSampleName, stringSampleFileContent);
                stringSampleName = $"{stringFilename2} 02{mainPage.stringExtensionTxt}";
                await CreateSampleFileAsync(mainPage.storageFolderLocker, stringSampleName, stringSampleFileContent);
                stringSampleName = $"{stringFoldername} 1";
                storageFolderResult = await CreateSampleFolderAsync(mainPage.storageFolderLocker, stringSampleName);
                if (storageFolderResult != null)
                {
                    stringSampleName = $"{stringFilename2} 03{mainPage.stringExtensionTxt}";
                    await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                    stringSampleName = $"{stringFilename2} 04{mainPage.stringExtensionTxt}";
                    await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                    stringSampleName = $"{stringFoldername} 2";
                    storageFolderResult = await CreateSampleFolderAsync(storageFolderResult, stringSampleName);
                    if (storageFolderResult != null)
                    {
                        stringSampleName = $"{stringFilename2} 05{mainPage.stringExtensionTxt}";
                        await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                        stringSampleName = $"{stringFilename2} 06{mainPage.stringExtensionTxt}";
                        await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                    }
                }
                // Create NoArchive folder in Locker folder and create sample folders and files in it.
                storageFolderResult = await mainPage.FolderCheckNoArchiveAsync();
                if (storageFolderResult != null)
                {
                    stringSampleName = $"{stringFilename2} 07{mainPage.stringExtensionTxt}";
                    await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                    stringSampleName = $"{stringFilename2} 08{mainPage.stringExtensionTxt}";
                    await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                    stringSampleName = $"{stringFoldername} 3";
                    storageFolderResult = await CreateSampleFolderAsync(storageFolderResult, stringSampleName);
                    if (storageFolderResult != null)
                    {
                        stringSampleName = $"{stringFilename2} 09{mainPage.stringExtensionTxt}";
                        await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                        stringSampleName = $"{stringFilename2} 10{mainPage.stringExtensionTxt}";
                        await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                        stringSampleName = $"{stringFoldername} 4";
                        storageFolderResult = await CreateSampleFolderAsync(storageFolderResult, stringSampleName);
                        if (storageFolderResult != null)
                        {
                            stringSampleName = $"{stringFilename2} 11{mainPage.stringExtensionTxt}";
                            await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                            stringSampleName = $"{stringFilename2} 12{mainPage.stringExtensionTxt}";
                            await CreateSampleFileAsync(storageFolderResult, stringSampleName, stringSampleFileContent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("SetupFolder.CreateSamplesItemsAsync()", ex.GetType()));
                throw;
            }
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// On page load set focus to ButSFFolderPicker.
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
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButFolderPicker,
                ButContinue
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            LibMPC.OutputMsgSuccess(TblkPageTitle, mainPage.resourceLoader.GetString("SF_TblkPageTitle"));
            CboxSamples.Content = mainPage.resourceLoader.GetString("SF_CboxSamples");
            if (mainPage.boolVerbose)   // Display long message.
                LibMPC.OutputMsgBright(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_SF_List_TblkPageMsg_Text_Long, EnumStringSeparator.TwoNewlines));  // Do not assemble string until needed to save memory.
            else                        // Display short message.
                LibMPC.OutputMsgBright(TblkPageMsg, LibMPC.JoinListString(Translate.TRS_SF_List_TblkPageMsg_Text_Short, EnumStringSeparator.TwoSpaces));   // Do not assemble string until needed to save memory.
            TblkResult.Text = string.Empty;                     // Show empty string until a value is set by App.
            CboxSamples.Visibility = Visibility.Collapsed;      // Hide CboxSamples until needed.
            LibMPC.ButtonVisibility(ButContinue, false);        // Hide ButContinue until needed.
            DataStoreFirstRun();
            //Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            ButFolderPicker.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Ask User to create folder and edit name to use as their Encryption Locker.
        /// User's folder will then be added to 'FutureAccessList' for retrieval on subsequent runs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButFolderPicker_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            try
            {
                CboxSamples.Visibility = Visibility.Collapsed;      // Hide checkbox until needed.
                LibMPC.ButtonVisibility(ButContinue, false);        // Hide continue button until needed.
                FolderPicker folderPicker = new FolderPicker() { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
                folderPicker.FileTypeFilter.Add("*");   // Need at least one filter to prevent exception.
                StorageFolder storageFolderPicked = await folderPicker.PickSingleFolderAsync();
                if (storageFolderPicked != null)
                {
                    // Folder picker can create a folder anywhere.  Example, 'C:\Program Files'.  But that does not mean App can get Read/Write access to it.  If folder
                    // location is protected, a 'System.UnauthorizedAccessException' error will occur when attempt is made to write data there.  App needs to check if it
                    // can get R/W access by attempting to create a file there.  if exception occurs, folder is in protected location. Make User pick a different location.
                    // Do not need to translate this since should only exist for an instant.
                    StorageFile storageFileFolderTest = await storageFolderPicked.CreateFileAsync("EL_XYZ_FILE_TEST.txt", CreationCollisionOption.ReplaceExisting);
                    if (storageFileFolderTest != null)
                        await storageFileFolderTest.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    // If above file did not throw exception, then App has R/W access to folder.  So continue...
                    mainPage.storageFolderLocker = storageFolderPicked;
                    mainPage.stringLockerPath = mainPage.storageFolderLocker.Path;  // Save path since used often.
                                                                                    // Application now has Read/Write access to contents 'inside' picked folder.  INSIDE is key word here.  App cannot delete or rename picked folder.
                    StorageApplicationPermissions.FutureAccessList.Clear();     // Empty FutureAccessList and start fresh.
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(mainPage.stringTokenLocker, mainPage.storageFolderLocker);   //Add new token to FutureAccessList.
                    List<string> list_stringMessagePage = new List<string>()
                        {
                            mainPage.resourceLoader.GetString("SF_Success_Picker_Msg1"),   // Folder selected was:
                            mainPage.stringLockerPath,
                            mainPage.resourceLoader.GetString("SF_Success_Picker_Msg2")    // Click following button to use this folder.  Click above button to select different folder.
                        };
                    LibMPC.OutputMsgSuccess(TblkResult, LibMPC.JoinListString(list_stringMessagePage, EnumStringSeparator.OneNewline));
                    CboxSamples.Visibility = Visibility.Visible;    // Show checkbox only if verbose mode?????
                    LibMPC.ButtonVisibility(ButContinue, true);     // Show continue button.
                    ButContinue.Focus(FocusState.Programmatic);
                }
                else    // User did not select a folder.
                {
                    LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("UMP_Error_Picker_NothingPicked"));    // Did not pick anything.
                    ButFolderPicker.Focus(FocusState.Programmatic);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Catch 'UnauthorizedAccessException' exception if User picks protected location for Locker folder.
                // This exception can be triggered by attempting to create the test file in a protected folder. Example, try to create Locker in C:\Program Files.
                // Cannot get read and write access to selected folder.  Folder likely in protected location.  Create folder in different location.
                LibMPC.OutputMsgError(TblkResult, mainPage.resourceLoader.GetString("SF_Error_Picker_FolderException"));
                CboxSamples.Visibility = Visibility.Collapsed;      // Hide checkbox.
                LibMPC.ButtonVisibility(ButContinue, false);        // Hide continue button.
                throw;
            }
            catch (Exception ex)    // Catch any other exceptions.
            {
                LibMPC.OutputMsgError(TblkResult, mainPage.UnhandledExceptionMessage("SetupFolder.ButFolderPicker_Click()", ex.GetType()));
                throw;
            }
        }

        /// <summary>
        /// User has accepted locker folder location so continue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButContinue_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Create sample items in Locker folder if CheckBox is checked.
            if (CboxSamples.IsChecked == true)
            {
                await CreateSamplesItemsAsync();
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolCboxSamples] = true;
            }
            else
            {
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolCboxSamples] = false;
            }
            if (boolFirstRun)
            {
                // After App first run, uncheck the box as default. After that, use previous setting.
                mainPage.applicationDataContainer.Values[mainPage.ds_BoolCboxSamples] = false;
            }
            mainPage.applicationDataContainer.Values[mainPage.ds_BoolDeleteSecure] = false;   // Create/reset setting in data store.
            mainPage.ShowPageSetupPassword();   // Navigate to next setup page to set up password.
        }

    }
}