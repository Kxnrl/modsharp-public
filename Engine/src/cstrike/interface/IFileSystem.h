/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

#ifndef CSTRIKE_INTERFACE_IFILESYSTEM_H
#define CSTRIKE_INTERFACE_IFILESYSTEM_H

#include "cstrike/interface/IAppSystem.h"
#include "cstrike/type/CUtlVector.h"

#include <cstdint>
#include <cstdio>

class CBufferString;
class CUtlString;
class KeyValues;
class CUtlBuffer;

using FileHandle_t    = void*;
using PathTypeQuery_t = uint32_t;

#define FILESYSTEM_MAX_SEARCH_PATHS 128

enum FileSystemSeek_t
{
    FILESYSTEM_SEEK_HEAD    = SEEK_SET,
    FILESYSTEM_SEEK_CURRENT = SEEK_CUR,
    FILESYSTEM_SEEK_TAIL    = SEEK_END,
};

enum
{
    FILESYSTEM_INVALID_FIND_HANDLE = -1
};

enum FilesystemMountRetval_t
{
    FILESYSTEM_MOUNT_OK = 0,
    FILESYSTEM_MOUNT_FAILED,
};

enum SearchPathAdd_t
{
    PATH_ADD_TO_HEAD,         // First path searched
    PATH_ADD_TO_TAIL,         // Last path searched
    PATH_ADD_TO_TAIL_ATINDEX, // First path searched
};

enum SearchPathPriority_t
{
    SEARCH_PATH_PRIORITY_DEFAULT     = 0,
    SEARCH_PATH_PRIORITY_LOOSE_FILES = 1,
    SEARCH_PATH_PRIORITY_VPK         = 2,
};

enum GetSearchPathTypes_t
{
    GET_SEARCH_PATH_NO_PACK_FILES   = 0,
    GET_SEARCH_PATH_NO_AUTO_MOUNTED = 1,
    GET_SEARCH_PATH_ALL             = 2,
};

enum FilesystemOpenExFlags_t
{
    FSOPEN_UNBUFFERED      = (1 << 0),
    FSOPEN_FORCE_TRACK_CRC = (1 << 1), // This makes it calculate a CRC for the file (if the file came from disk) regardless
                                       // of the IFileList passed to RegisterFileWhitelist.
    FSOPEN_NEVERINPACK = (1 << 2),     // 360 only, hint to FS that file is not allowed to be in pack file
};

// search path filtering
enum PathTypeFilter_t
{
    FILTER_NONE          = 0, // no filtering, all search path types match
    FILTER_CULLLOCALIZED = 1,
    FILTER_CULLPACK      = 2, // pack based search paths are culled (maps and zips)
    FILTER_CULLNONPACK   = 3, // non-pack based search paths are culled
};

struct FileFindHandle_t
{
    IFileSystem* m_pFileSystem;
    intptr_t     m_nFindDataIndex;
};

// TODO: 用gamedata
class IFileSystem : public IAppSystem
{
public:
    //--------------------------------------------------------
    // IBaseFileSystem
    //--------------------------------------------------------
    virtual int Read(void* pOutput, uint32_t size, FileHandle_t file) = 0;       // 12
    virtual int Write(const void* pInput, uint32_t size, FileHandle_t file) = 0; // 13

    // if pathID is NULL, all paths will be searched for the file
    virtual FileHandle_t Open(const char* pFileName, const char* pOptions, const char* pathID = nullptr) = 0; // 14
    virtual void         Close(FileHandle_t file) = 0;                                                        // 15

    virtual void     Seek(FileHandle_t file, int pos, FileSystemSeek_t seekType) = 0; // 16
    virtual uint32_t Tell(FileHandle_t file) = 0;                                     // 17
    virtual uint32_t Size(FileHandle_t file) = 0;                                     // 18
    virtual uint32_t Size(const char* pFileName, const char* pPathID = nullptr) = 0;  // 19

    virtual void Flush(FileHandle_t file) = 0;                                       // 20
    virtual bool Precache(const char* pFileName, const char* pPathID = nullptr) = 0; // 21

    virtual bool FileExists(const char* pFileName, const char* pPathID = nullptr) = 0;                     // 22
    virtual bool IsFileWritable(const char* pFileName, const char* pPathID = nullptr) = 0;                 // 23
    virtual bool SetFileWritable(const char* pFileName, bool writable, const char* pPathID = nullptr) = 0; // 24

    virtual long GetFileTime(const char* pFileName, const char* pPathID = nullptr) = 0; // 25

    //--------------------------------------------------------
    // Reads/writes files to utlbuffers. Use this for optimal read performance when doing open/read/close
    //--------------------------------------------------------
    virtual bool ReadFile(const char* pFileName, const char* pPathID, CUtlBuffer& buf, int nMaxBytes = 0, int nStartingByte = 0, void* pfnAlloc = NULL) = 0; // 26
    virtual bool WriteFile(const char* pFileName, const char* pPathID, CUtlBuffer& buf) = 0;                                                                 // 27
    virtual bool UnzipFile(const char* pFileName, const char* pPathID, const char* pDestination) = 0;                                                        // 28
    virtual bool CopyAFile(const char* pFileName, const char* pPathID, const char* pDestination, bool bDontOverwrite = false) = 0;                           // 29

    // --------------------
    // FileSystem
    // --------------------

    virtual void unk001(FileHandle_t, bool) = 0;                         // 30
    virtual void unk001(const char* pFileName, void* pPathID, bool) = 0; // 31

    //--------------------------------------------------------
    // Search path manipulation
    //--------------------------------------------------------

    // Add paths in priority order (mod dir, game dir, ....)
    // If one or more .pak files are in the specified directory, then they are
    //  added after the file system path
    // If the path is the relative path to a .bsp file, then any previous .bsp file
    //  override is cleared and the current .bsp is searched for an embedded PAK file
    //  and this file becomes the highest priority search path ( i.e., it's looked at first
    //   even before the mod's file system path ).
    virtual void AddSearchPath(const char* pPath, const char* pathID, SearchPathAdd_t addType = PATH_ADD_TO_TAIL, SearchPathPriority_t priority = SEARCH_PATH_PRIORITY_DEFAULT, int unknown = 0) = 0; // 32
    virtual bool RemoveSearchPath(const char* pPath, const char* pathID = nullptr) = 0;                                                                                                               // 33

    virtual void* SaveSearchPathState(const char* pszName) const = 0; // 34
    virtual void  RestoreSearchPathState(void* pState) = 0;           // 35
    virtual void  DestroySearchPathState(void* pState) = 0;           // 36

    // Remove all search paths (including write path?)
    virtual void RemoveAllSearchPaths(void) = 0; // 37

    // Remove search paths associated with a given pathID
    virtual void RemoveSearchPaths(const char* szPathID) = 0; // 38

    // This is for optimization. If you mark a path ID as "by request only", then files inside it
    // will only be accessed if the path ID is specifically requested. Otherwise, it will be ignored.
    // If there are currently no search paths with the specified path ID, then it will still
    // remember it in case you add search paths with this path ID.
    virtual void MarkPathIDByRequestOnly(const char* pPathID, bool bRequestOnly) = 0; // 39

    virtual bool IsFileInReadOnlySearchPath(const char* pPathID, const char* pFileName) = 0;  // 40
    virtual void SetSearchPathReadOnly(const char* pPathID, const char*, bool bReadOnly) = 0; // 41

    // converts a partial path into a full path
    virtual const char* RelativePathToFullPath(const char* pFileName, const char* pPathID, void* pLocalPath, PathTypeFilter_t pathFilter = FILTER_NONE, PathTypeQuery_t* pPathType = NULL) = 0; // 42

    virtual bool GetWritePath(const char*, const char*, void*) = 0; // 43

    // Returns the nSearchPathsToGet amount of paths, each path is separated by ;s. Returns true if pathID has any paths
    virtual bool GetSearchPath(const char* pathID, GetSearchPathTypes_t pathType, CBufferString* pPath, int nSearchPathsToGet) = 0; // 44
    virtual bool AddPackFile(const char* fullpath, const char* pathID) = 0;                                                         // 45

    virtual void unk002(int, void*) = 0; // 46

    //--------------------------------------------------------
    // File manipulation operations
    //--------------------------------------------------------

    // Deletes a file (on the WritePath)
    virtual void RemoveFile(const char* pRelativePath, const char* pathID = nullptr) = 0; // 47

    // Renames a file (on the WritePath)
    virtual bool RenameFile(const char* pOldPath, const char* pNewPath, const char* pathID = nullptr) = 0; // 48

    // create a local directory structure
    virtual void CreateDirHierarchy(const char* path, const char* pathID = nullptr) = 0;   // 49
    virtual void CreateDirHierarchyForFile(const char* pFileName, const char* pathID) = 0; // 50

    // File I/O and info
    virtual bool IsDirectory(const char* pFileName, const char* pathID = nullptr) = 0; // 51

    virtual void FileTimeToString(char* pStrip, int maxCharsIncludingTerminator, long fileTime) = 0; // 52

    //--------------------------------------------------------
    // Open file operations
    //--------------------------------------------------------

    virtual void SetBufferSize(FileHandle_t file, unsigned nBytes) = 0; // 53

    virtual bool IsOk(FileHandle_t file) = 0; // 54

    virtual bool EndOfFile(FileHandle_t file) = 0; // 55

    virtual char* ReadLine(char* pOutput, int maxChars, FileHandle_t file) = 0; // 56

    virtual int FPrintf(FileHandle_t file, const char* pFormat, ...) = 0; // 57

    //--------------------------------------------------------
    // Dynamic library operations
    //--------------------------------------------------------

    // load/unload modules
    virtual void** LoadModule(const char* pFileName, const char* pPathID = nullptr, bool bValidatedDllOnly = true) = 0; // 58
    virtual void   UnloadModule(void* pModule) = 0;                                                                     // 59

    //--------------------------------------------------------
    // File searching operations
    //--------------------------------------------------------

    // FindFirst/FindNext. Also see FindFirstEx.
    virtual const char* FindFirst(const char* pWildCard, FileFindHandle_t* pHandle) = 0; // 60
    virtual const char* FindNext(FileFindHandle_t* handle) = 0;                          // 61
    virtual bool        FindIsDirectory(FileFindHandle_t* handle) = 0;                   // 62
    virtual void        FindClose(FileFindHandle_t* handle) = 0;                         // 63

    // Same as FindFirst, but you can filter by path ID, which can make it faster.
    virtual const char* FindFirstEx(const char* pWildCard, const char* pPathID, FileFindHandle_t* pHandle) = 0; // 64

    virtual void FindFileAbsoluteList(void* output, const char* pWildCard, const char* pPathID) = 0;

    //--------------------------------------------------------
    // File name and directory operations
    //--------------------------------------------------------

    // Returns true on success ( based on current list of search paths, otherwise false if
    //  it can't be resolved )
    virtual bool FullPathToRelativePath(const char* pFullpath, const char* pPathID, char* pRelative, int maxlen) = 0;

    // Gets the current working directory
    virtual bool GetCurrentDirectory(void* pDirectory) = 0;

    //--------------------------------------------------------
    // Filename dictionary operations
    //--------------------------------------------------------

    virtual void* FindOrAddFileName(const char* pFileName)           = 0;
    virtual bool  String(const void** handle, char* buf, int buflen) = 0;

    virtual void unk006() = 0;
    virtual void unk007() = 0;
    virtual void unk008() = 0;

    virtual void Trace_DumpUnclosedFiles() = 0;

    virtual void PrintSearchPaths(void) = 0;

    // output
    virtual void SetWarningFunc(void (*pfnWarning)(const char* fmt, ...)) = 0;
    virtual void SetWarningLevel(int level)                               = 0;

    // Returns the file system statistics retreived by the implementation.  Returns NULL if not supported.
    virtual const void* GetFilesystemStatistics() = 0;

    virtual FileHandle_t OpenEx(const char* pFileName, const char* pOptions, unsigned flags = 0, const char* pathID = nullptr) = 0;

    // Extended version of read provides more context to allow for more optimal reading
    virtual int ReadEx(void* pOutput, int sizeDest, int size, FileHandle_t file)                                                                                                                             = 0;
    virtual int ReadFileEx(const char* pFileName, const char* pPath, void** ppBuf, bool bNullTerminate = false, bool bOptimalAlloc = false, int nMaxBytes = 0, int nStartingByte = 0, void* pfnAlloc = NULL) = 0;

    virtual void* FindFileName(const char* pFileName) = 0;

#if defined(TRACK_BLOCKING_IO)
    virtual void EnableBlockingFileAccessTracking(bool state) = 0;
    virtual bool IsBlockingFileAccessEnabled() const          = 0;

    virtual IBlockingFileItemList* RetrieveBlockingFileAccessInfo() = 0;
#endif

    // Fixme, we could do these via a string embedded into the compiled data, etc...
    enum KeyValuesPreloadType_t
    {
        TYPE_VMT,
        TYPE_SOUNDEMITTER,
        TYPE_SOUNDSCAPE,
        TYPE_SOUNDOPERATORS,
        NUM_PRELOAD_TYPES
    };

    // If the "PreloadedData" hasn't been purged, then this'll try and instance the KeyValues using the fast path of compiled keyvalues loaded during startup.
    // Otherwise, it'll just fall through to the regular KeyValues loading routines
    virtual KeyValues* LoadKeyValues(KeyValuesPreloadType_t type, const char* filename, const char* pPathID = nullptr)                  = 0;
    virtual bool       LoadKeyValues(KeyValues* head, KeyValuesPreloadType_t type, const char* filename, const char* pPathID = nullptr) = 0;

    virtual bool GetFileTypeForFullPath(const char* pFullPath, wchar_t* buf, size_t bufSizeInBytes) = 0;

    //--------------------------------------------------------
    //--------------------------------------------------------
    virtual bool ReadToBuffer(FileHandle_t hFile, CUtlBuffer& buf, int nMaxBytes = 0, void* pfnAlloc = NULL) = 0;

    //--------------------------------------------------------
    // Optimal IO operations
    //--------------------------------------------------------
    virtual bool    GetOptimalIOConstraints(FileHandle_t hFile, unsigned* pOffsetAlign, unsigned* pSizeAlign, unsigned* pBufferAlign) = 0;
    inline unsigned GetOptimalReadSize(FileHandle_t hFile, unsigned nLogicalSize);
    virtual void*   AllocOptimalReadBuffer(FileHandle_t hFile, unsigned nSize = 0, unsigned nOffset = 0) = 0;
    virtual void    FreeOptimalReadBuffer(void*)                                                         = 0;

    //--------------------------------------------------------
    //
    //--------------------------------------------------------
    virtual int  GetPathIndex(const void** handle)                   = 0;
    virtual long GetPathTime(const char* pPath, const char* pPathID) = 0;

    virtual void GetSearchPathID(CBufferString* inout) = 0;

    // File path is "ugc:<UGCHandle_t value>"
    virtual void AddUGCVPKFile(const char* pszName, const char* pPathID, SearchPathAdd_t addType = PATH_ADD_TO_TAIL) = 0;
    virtual void RemoveUGCVPKFile(const char* pszName)                                                               = 0;
    virtual bool IsUGCVPKFileLoaded(const char* pszName)                                                             = 0;

    virtual void EnableAutoVPKFileLoading(bool) = 0;
    virtual bool GetAutoVPKFileLoading(void)    = 0;
    virtual bool ValidateLoadedVPKs(void)       = 0;

    virtual void LoadUGC(uint64_t ugcId, const char* pFilename, const char* pPathID) = 0;
    virtual void UnloadUGC(uint64_t ugcId, const char* pPathID)                      = 0;
    virtual bool IsUGCLoaded(uint64_t ugcId) const                                   = 0;

    virtual uint64_t     ParseUGCHandleFromFilename(const char* pFileName, char** ppRemainingFilename = nullptr) const                     = 0;
    virtual void         CreateFilenameForUGCFile(CBufferString& out, uint64_t ugcId, const char* pExtension, char separator = '\\') const = 0;
    virtual FileHandle_t OpenUGCFile(uint64_t ugcId)                                                                                       = 0;

    // call this to look for CPU-hogs during loading processes. When you set this, a breakpoint
    // will be issued whenever the indicated # of seconds go by without an i/o request.  Passing
    // 0.0 will turn off the functionality.
    virtual void SetIODelayAlarm(float flThreshhold) = 0;

    virtual bool DeleteDirectory(const char* pFileName, const char* pathID = nullptr)          = 0;
    virtual bool DeleteDirectoryAndContents_R(const char* pFileName, const char* pathID, bool) = 0;

    virtual bool IsPathInvalidForFilesystem(const char* pFileName) = 0;

    virtual void GetAvailableDrives(void* drives) = 0;

    virtual CUtlString ReadLine(FileHandle_t file, bool bStripNewline = true) = 0;

    virtual void GetSearchPathsForPathID(const char*, GetSearchPathTypes_t, CUtlVector<CUtlString>*) = 0;

    virtual void MarkContentCorrupt(bool bMissingFilesOnly, const char* pFile) = 0;
};
#endif