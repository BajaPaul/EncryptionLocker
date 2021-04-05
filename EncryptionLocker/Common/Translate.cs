using System.Collections.Generic;

// This file used to isolate and customize translations that can not be handled with simple call to mainPage.resourceLoader.GetString("Resource String").
// Each language translation is unique and will require tweaks that can be done in this method.
// This method is not overly inefficient since mostly creates new pointers to existing strings versus duplicating the strings.
//
// Languages supported: https://docs.microsoft.com/en-us/windows/uwp/publish/supported-languages
//
// A String object immutable (read-only) since value of it cannot be changed after created.
// Methods that appear to modify a String object actually return a new String object that contains the modifications and then deletes the original String object.
// Therefore, String objects are pointers to a series of characters stored elsewhere.
// More Info: https://msdn.microsoft.com/en-us/library/system.string(v=vs.110).aspx#Immutability

namespace EncryptionLocker.Common
{
    /// <summary>
    /// This class used to isolate and customize translations that can not be handled with simple call to mainPage.resourceLoader.GetString("Resource String").
    /// For this page, use 'TRS_' prefix, shorthand for 'Translate String'.
    /// </summary>
    public static class Translate
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private static readonly MainPage mainPage = MainPage.mainPagePointer;

        // MainPage (MP) page settings:

        public static List<string> TRS_MP_List_LockerResetMsgLong = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("UMP_LockerReset_Msg01"),
            mainPage.resourceLoader.GetString("UMP_Reset_Msg"),
            mainPage.resourceLoader.GetString("UMP_LockerReset_Msg02"),
            mainPage.resourceLoader.GetString("UMP_LockerReset_Msg03")
        };

        // SetupPassword (SP) page settings:

        public static List<string> TRS_SP_List_TblkPageMsg_Text_Long = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("UMP_SP_Msg02"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg01"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg04"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg03"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg05")
        };

        public static List<string> TRS_SP_List_TblkPageMsg_Text_Short = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("UMP_SP_Msg02"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg03"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg05")
        };

        // EnterPassword (EP) page settings:

        public static List<string> TRS_EP_Error_PasswordVerify = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("EP_Error_FilePath"),
            mainPage.resourceLoader.GetString("EP_Error_LockerReset_Required"),
            mainPage.resourceLoader.GetString("UMP_Reset_Msg")
        };

        public static List<string> TRS_EP_Error_PageLoad_Msg01 = new List<string>   // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("EP_Error_LockerAccess"),
            mainPage.resourceLoader.GetString("EP_Error_LockerReset_Required"),
            mainPage.resourceLoader.GetString("UMP_Reset_Msg")
        };

        public static List<string> TRS_EP_Error_PageLoad_Msg02 = new List<string>   // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("EP_Error_LockerNotFound"),
            mainPage.resourceLoader.GetString("UMP_Reset_Msg")
        };

        // AboutToggles (AT) page settings:

        public static List<string> TRS_AT_List_TblkToggleMsg_Text = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("UMP_ToggleExitApp_Msg"),
            mainPage.resourceLoader.GetString("UMP_ToggleDeleteSecure_Msg"),
            mainPage.resourceLoader.GetString("UMP_ToggleVerbose_Msg")
        };

        // About (AB) page settings:

        public static List<string> TRS_AB_List_TblkProgrammer_Text = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("AB_TblkProgrammer"),
            mainPage.stringAppVersion       // Get current App version from MainPage.
        };

        public static List<string> TRS_AB_List_TblkPageMsg_Text = new List<string>  // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("UMP_AB_Msg01"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg01"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg02"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg03"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg04"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg05"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg06"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg07"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg08"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg09"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg10"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg11")
        };

        // SetupFolder (SF) page settings:

        public static List<string> TRS_SF_List_TblkPageMsg_Text_Long = new List<string>     // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("SF_Msg02"),
            mainPage.resourceLoader.GetString("SF_Msg03"),
            mainPage.resourceLoader.GetString("SF_Msg06"),
            mainPage.resourceLoader.GetString("SF_Msg04"),
            mainPage.resourceLoader.GetString("SF_Msg05")
        };

        public static List<string> TRS_SF_List_TblkPageMsg_Text_Short = new List<string>    // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("SF_Msg01"),
            mainPage.resourceLoader.GetString("SF_Msg06")
        };

        public static List<string> TRS_SF_List_SampleFile_Content = new List<string>        // Do not assemble string until needed to save memory.
        {
            mainPage.resourceLoader.GetString("SF_CreateSample_Intro"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg01"),
            mainPage.resourceLoader.GetString("UMP_SP_Msg01"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg02"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg03"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg04"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg05"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg06"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg07"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg08"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg09"),
            mainPage.resourceLoader.GetString("SF_Msg03"),
            mainPage.resourceLoader.GetString("SF_Msg06"),
            mainPage.resourceLoader.GetString("SF_Msg04"),
            mainPage.resourceLoader.GetString("UMP_LockerReset_Msg01"),
            mainPage.resourceLoader.GetString("UMP_Reset_Msg"),
            mainPage.resourceLoader.GetString("UMP_LockerReset_Msg02"),
            mainPage.resourceLoader.GetString("UMP_ToggleExitApp_Msg"),
            mainPage.resourceLoader.GetString("UMP_ToggleDeleteSecure_Msg"),
            mainPage.resourceLoader.GetString("UMP_ToggleVerbose_Msg"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg10"),
            mainPage.resourceLoader.GetString("UMP_AB_Msg11"),
            mainPage.resourceLoader.GetString("SF_CreateSample_Closing")
        };

        // HomePage (HP) page settings:

        // Note: HP_Timer = ({0:N2} seconds) -- The 0 in string will conflict with any earlier strings that have 0 in them and then will not return result.
        // Work-a-round is to encapsule this in separate string via Format(String.Format(Translate.TRS_HP_TimeSpanElapsed, timeSpanElapsed.TotalSeconds)).
        public static string TRS_HP_TimeSpanElapsed = mainPage.resourceLoader.GetString("HP_Timer");          // **** May need to adjust string for culture ****

    }
}
