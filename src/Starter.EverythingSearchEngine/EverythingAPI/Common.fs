module Starter.EverythingSearchEngine.EverythingAPI

open System

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

type IEverything =
    abstract member SetSearch : string -> uint32
    abstract member SetRequestFlags : uint32 -> unit
    abstract member SetMax : uint32 -> unit
    abstract member Query : bool -> bool
    abstract member GetNumResults : unit -> uint32
    abstract member GetResultFileName : uint -> IntPtr
    abstract member GetResultFullPathName : uint -> System.Text.StringBuilder -> uint -> unit

type Everything32() =
    interface IEverything with
        member this.SetSearch lpSearchString = EverythingAPI_x86.Everything_SetSearchW lpSearchString
        member this.SetRequestFlags dwRequestFlags = EverythingAPI_x86.Everything_SetRequestFlags dwRequestFlags
        member this.SetMax max = EverythingAPI_x86.Everything_SetMax max
        member this.Query wait = EverythingAPI_x86.Everything_QueryW wait
        member this.GetNumResults() = EverythingAPI_x86.Everything_GetNumResults()
        member this.GetResultFileName index = EverythingAPI_x86.Everything_GetResultFileName index
        member this.GetResultFullPathName index lpString maxCount = EverythingAPI_x86.Everything_GetResultFullPathName(index, lpString, maxCount)

type Everything64() =
    interface IEverything with
        member this.SetSearch lpSearchString = EverythingAPI_x64.Everything_SetSearchW lpSearchString
        member this.SetRequestFlags dwRequestFlags = EverythingAPI_x64.Everything_SetRequestFlags dwRequestFlags
        member this.SetMax max = EverythingAPI_x64.Everything_SetMax max
        member this.Query wait = EverythingAPI_x64.Everything_QueryW wait
        member this.GetNumResults() = EverythingAPI_x64.Everything_GetNumResults()
        member this.GetResultFileName index = EverythingAPI_x64.Everything_GetResultFileName index
        member this.GetResultFullPathName index lpString maxCount = EverythingAPI_x64.Everything_GetResultFullPathName(index, lpString, maxCount)

type EverythingArm64() =
    interface IEverything with
        member this.SetSearch lpSearchString = EverythingAPI_arm64.Everything_SetSearchW lpSearchString
        member this.SetRequestFlags dwRequestFlags = EverythingAPI_arm64.Everything_SetRequestFlags dwRequestFlags
        member this.SetMax max = EverythingAPI_arm64.Everything_SetMax max
        member this.Query wait = EverythingAPI_arm64.Everything_QueryW wait
        member this.GetNumResults() = EverythingAPI_arm64.Everything_GetNumResults()
        member this.GetResultFileName index = EverythingAPI_arm64.Everything_GetResultFileName index
        member this.GetResultFullPathName index lpString maxCount = EverythingAPI_arm64.Everything_GetResultFullPathName(index, lpString, maxCount)
