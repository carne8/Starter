module Starter.EverythingSearchEngine.EverythingAPI_x64

open System
open System.Text
open System.Runtime.InteropServices

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
