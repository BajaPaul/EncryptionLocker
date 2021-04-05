EncryptionLocker:

This C# UWP App encrypts and decrypts folders and files using the Advanced Encryption Standard (AES).  Data is stored independent of application and will not be deleted if application is uninstalled.  Decrypted data is accessible by any application installed on device.
 
Application provides two methods to encrypt and decrypt data.  Each method has advantages and disadvantages.  Choose which method to use depending on data size and intended use.

First method places folder or file into an archive file and then encrypts archive file.  Files in folder cannot be seen since they were placed into one file.  Sharing content is simple since only one file needs to be exchanged.  Limitation of this method is it creates one large file to encrypt.  This method will exceed device memory limits more often than second method since encrypting one large file versus many smaller files.  This method is always slower than second method.

Second method encrypts individual files located in hierarchy of AES folder.  This method is always faster than first method since folders are not archived into one file.    Folders of any size can be encrypted.  File will not be encrypted if it exceeds device limit discussed above.  Application will show list of files not encrypted.  Limitation of this method is files in a folder are visible.  This limitation can be minimized by using generic folder and file names such as Folder01 and File01.  Sharing data using this method is more complex since multiple folders and files need to be exchanged.

This App requires library files LibMainPageCommon.cs, LibZipArchive.cs, LibAesEncryption.cs, and LibUtilitiesFile.cs located in Library-Files-for-other-Repository-Apps to be linked under folder named 'Libraries' to compile.

I no longer intend to support this App so placing code on GitHub so others can use if useful.

