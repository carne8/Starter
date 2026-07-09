module Starter.TextMatching.Memory

/// Two region of memory used in by the fuzzy finding algorithm
type Slab =
    { i16: int16 array
      i32: int32 array }

    // Same as fzf slabs
    static let [<Literal>] SLAB_16_SIZE = 100 * 1024 // 200 Ko
    static let [<Literal>] SLAB_32_SIZE = 2048 // 8 Ko

    static member createDefault () =
        { i16 = Array.zeroCreate SLAB_16_SIZE
          i32 = Array.zeroCreate SLAB_32_SIZE }
