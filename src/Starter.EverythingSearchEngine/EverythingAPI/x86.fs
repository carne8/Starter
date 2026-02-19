module Starter.EverythingSearchEngine.EverythingAPI_x86

open System
open System.Text
open System.Runtime.InteropServices

[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern uint32 Everything_SetSearchW(string lpSearchString)
[<DllImport("Everything32.dll")>]
extern void Everything_SetMatchPath(bool bEnable)
[<DllImport("Everything32.dll")>]
extern void Everything_SetMatchCase(bool bEnable)
[<DllImport("Everything32.dll")>]
extern void Everything_SetMatchWholeWord(bool bEnable)
[<DllImport("Everything32.dll")>]
extern void Everything_SetRegex(bool bEnable)
[<DllImport("Everything32.dll")>]
extern void Everything_SetMax(uint32 dwMax)
[<DllImport("Everything32.dll")>]
extern void Everything_SetOffset(uint32 dwOffset)

[<DllImport("Everything32.dll")>]
extern bool Everything_GetMatchPath()
[<DllImport("Everything32.dll")>]
extern bool Everything_GetMatchCase()
[<DllImport("Everything32.dll")>]
extern bool Everything_GetMatchWholeWord()
[<DllImport("Everything32.dll")>]
extern bool Everything_GetRegex()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetMax()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetOffset()
[<DllImport("Everything32.dll")>]
extern IntPtr Everything_GetSearchW()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetLastError()

[<DllImport("Everything32.dll")>]
extern bool Everything_QueryW(bool bWait)

[<DllImport("Everything32.dll")>]
extern void Everything_SortResultsByPath()

[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetNumFileResults()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetNumFolderResults()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetNumResults()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetTotFileResults()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetTotFolderResults()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetTotResults()
[<DllImport("Everything32.dll")>]
extern bool Everything_IsVolumeResult(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern bool Everything_IsFolderResult(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern bool Everything_IsFileResult(uint32 nIndex)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern void Everything_GetResultFullPathName(uint32 nIndex, StringBuilder lpString, uint32 nMaxCount)
[<DllImport("Everything32.dll")>]
extern void Everything_Reset()

[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultFileName(uint32 nIndex)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultPath(uint32 nIndex)

// Everything 1.4
[<DllImport("Everything32.dll")>]
extern void Everything_SetSort(uint32 dwSortType)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetSort()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetResultListSort()
[<DllImport("Everything32.dll")>]
extern void Everything_SetRequestFlags(uint32 dwRequestFlags)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetRequestFlags()
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetResultListRequestFlags()
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultExtension(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultSize(uint32 nIndex, int64& lpFileSize)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultDateCreated(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultDateModified(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultDateAccessed(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetResultAttributes(uint32 nIndex)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultFileListFileName(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetResultRunCount(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultDateRun(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything32.dll")>]
extern bool Everything_GetResultDateRecentlyChanged(uint32 nIndex, int64& lpFileTime)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedFileName(uint32 nIndex)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedPath(uint32 nIndex)
[<DllImport("Everything32.dll", CharSet = CharSet.Unicode)>]
extern IntPtr Everything_GetResultHighlightedFullPathAndFileName(uint32 nIndex)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_GetRunCountFromFileName(string lpFileName)
[<DllImport("Everything32.dll")>]
extern bool Everything_SetRunCountFromFileName(string lpFileName, uint32 dwRunCount)
[<DllImport("Everything32.dll")>]
extern uint32 Everything_IncRunCountFromFileName(string lpFileName)
