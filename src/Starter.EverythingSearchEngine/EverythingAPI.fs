module Starter.EverythingSearchEngine.EverythingAPI

open System
open System.Text
open System.Runtime.InteropServices

type CallResult =
    | EVERYTHING_OK = 0u
    | EVERYTHING_ERROR_MEMORY = 1u
    | EVERYTHING_ERROR_IPC = 2u
    | EVERYTHING_ERROR_REGISTERCLASSEX = 3u
    | EVERYTHING_ERROR_CREATEWINDOW = 4u
    | EVERYTHING_ERROR_CREATETHREAD = 5u
    | EVERYTHING_ERROR_INVALIDINDEX = 6u
    | EVERYTHING_ERROR_INVALIDCALL = 7u

module RequestFlags =
    let [<Literal>] FILE_NAME = 0x00000001u
    let [<Literal>] PATH = 0x00000002u
    let [<Literal>] FULL_PATH_AND_FILE_NAME = 0x00000004u
    let [<Literal>] EXTENSION = 0x00000008u
    let [<Literal>] SIZE = 0x00000010u
    let [<Literal>] DATE_CREATED = 0x00000020u
    let [<Literal>] DATE_MODIFIED = 0x00000040u
    let [<Literal>] DATE_ACCESSED = 0x00000080u
    let [<Literal>] ATTRIBUTES = 0x00000100u
    let [<Literal>] FILE_LIST_FILE_NAME = 0x00000200u
    let [<Literal>] RUN_COUNT = 0x00000400u
    let [<Literal>] DATE_RUN = 0x00000800u
    let [<Literal>] DATE_RECENTLY_CHANGED = 0x00001000u
    let [<Literal>] HIGHLIGHTED_FILE_NAME = 0x00002000u
    let [<Literal>] HIGHLIGHTED_PATH = 0x00004000u
    let [<Literal>] HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000u

module Sort =
    let [<Literal>] NAME_ASCENDING = 1u
    let [<Literal>] NAME_DESCENDING = 2u
    let [<Literal>] PATH_ASCENDING = 3u
    let [<Literal>] PATH_DESCENDING = 4u
    let [<Literal>] SIZE_ASCENDING = 5u
    let [<Literal>] SIZE_DESCENDING = 6u
    let [<Literal>] EXTENSION_ASCENDING = 7u
    let [<Literal>] EXTENSION_DESCENDING = 8u
    let [<Literal>] TYPE_NAME_ASCENDING = 9u
    let [<Literal>] TYPE_NAME_DESCENDING = 10u
    let [<Literal>] DATE_CREATED_ASCENDING = 11u
    let [<Literal>] DATE_CREATED_DESCENDING = 12u
    let [<Literal>] DATE_MODIFIED_ASCENDING = 13u
    let [<Literal>] DATE_MODIFIED_DESCENDING = 14u
    let [<Literal>] ATTRIBUTES_ASCENDING = 15u
    let [<Literal>] ATTRIBUTES_DESCENDING = 16u
    let [<Literal>] FILE_LIST_FILENAME_ASCENDING = 17u
    let [<Literal>] FILE_LIST_FILENAME_DESCENDING = 18u
    let [<Literal>] RUN_COUNT_ASCENDING = 19u
    let [<Literal>] RUN_COUNT_DESCENDING = 20u
    let [<Literal>] DATE_RECENTLY_CHANGED_ASCENDING = 21u
    let [<Literal>] DATE_RECENTLY_CHANGED_DESCENDING = 22u
    let [<Literal>] DATE_ACCESSED_ASCENDING = 23u
    let [<Literal>] DATE_ACCESSED_DESCENDING = 24u
    let [<Literal>] DATE_RUN_ASCENDING = 25u
    let [<Literal>] DATE_RUN_DESCENDING = 26u

let [<Literal>] EVERYTHING_TARGET_MACHINE_X86 = 1
let [<Literal>] EVERYTHING_TARGET_MACHINE_X64 = 2
let [<Literal>] EVERYTHING_TARGET_MACHINE_ARM = 3

[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern uint32 Everything_SetSearchW(string lpSearchString)
[<DllImport("Everything64.dll")>]
extern void Everything_SetMatchPath(bool bEnable)
[<DllImport("Everything64.dll")>]
extern void Everything_SetMatchCase(bool bEnable)
[<DllImport("Everything64.dll")>]
extern void Everything_SetMatchWholeWord(bool bEnable)
[<DllImport("Everything64.dll")>]
extern void Everything_SetRegex(bool bEnable)
[<DllImport("Everything64.dll")>]
extern void Everything_SetMax(uint32 dwMax)
[<DllImport("Everything64.dll")>]
extern void Everything_SetOffset(uint32 dwOffset)

[<DllImport("Everything64.dll")>]
extern bool Everything_GetMatchPath()
[<DllImport("Everything64.dll")>]
extern bool Everything_GetMatchCase()
[<DllImport("Everything64.dll")>]
extern bool Everything_GetMatchWholeWord()
[<DllImport("Everything64.dll")>]
extern bool Everything_GetRegex()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetMax()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetOffset()
[<DllImport("Everything64.dll")>]
extern IntPtr Everything_GetSearchW()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetLastError()

[<DllImport("Everything64.dll")>]
extern bool Everything_QueryW(bool bWait)

[<DllImport("Everything64.dll")>]
extern void Everything_SortResultsByPath()

[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetNumFileResults()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetNumFolderResults()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetNumResults()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetTotFileResults()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetTotFolderResults()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetTotResults()
[<DllImport("Everything64.dll")>]
extern bool Everything_IsVolumeResult(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern bool Everything_IsFolderResult(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern bool Everything_IsFileResult(uint32 nIndex)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern void Everything_GetResultFullPathName(uint32 nIndex, StringBuilder lpString, uint32 nMaxCount)
[<DllImport("Everything64.dll")>]
extern void Everything_Reset()

[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultFileName(uint32 nIndex)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultPath(uint32 nIndex)

// Everything 1.4
[<DllImport("Everything64.dll")>]
extern void Everything_SetSort(uint32 dwSortType)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetSort()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetResultListSort()
[<DllImport("Everything64.dll")>]
extern void Everything_SetRequestFlags(uint32 dwRequestFlags)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetRequestFlags()
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetResultListRequestFlags()
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultExtension(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultSize(uint32 nIndex, int64& lpFileSize)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultDateCreated(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultDateModified(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultDateAccessed(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetResultAttributes(uint32 nIndex)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultFileListFileName(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetResultRunCount(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultDateRun(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything64.dll")>]
extern bool Everything_GetResultDateRecentlyChanged(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedFileName(uint32 nIndex)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedPath(uint32 nIndex)
[<DllImport("Everything64.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedFullPathAndFileName(uint32 nIndex)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_GetRunCountFromFileName(string lpFileName)
[<DllImport("Everything64.dll")>]
extern bool Everything_SetRunCountFromFileName(string lpFileName, uint32 dwRunCount)
[<DllImport("Everything64.dll")>]
extern uint32 Everything_IncRunCountFromFileName(string lpFileName)
