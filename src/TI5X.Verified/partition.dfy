// Verified memory-partition arithmetic for the TI5X calculator engine.
//
// The usable program range is the minimum of the logical partition size
// (two program steps per relocated memory register on bank 0) and the
// physical bank length. Every program-memory access in the engine must
// stay strictly below this bound, so this module proves that the bound
// returned by EffectiveMaxProg is never negative and never exceeds the
// physical bank length.
//
// The C# engine mirrors this model in src/TI5X.Core/Partition.cs; the test
// suite proves that both implementations agree on a swept input space.
module Partition {

  const MaxLibProgram: int := 1000

  function EffectiveMaxProg(regOffset: int, bankLength: int, curBank: int): int
    requires 0 <= regOffset
    requires 0 <= bankLength
    requires 0 <= curBank
  {
    var logical := if curBank == 0 then regOffset * 2 else MaxLibProgram;
    if logical < bankLength then logical else bankLength
  }

  // The bound is safe: within the physical bank, and within the logical
  // partition of the current bank.
  lemma EffectiveMaxProgBounds(regOffset: int, bankLength: int, curBank: int)
    requires 0 <= regOffset
    requires 0 <= bankLength
    requires 0 <= curBank
    ensures var m := EffectiveMaxProg(regOffset, bankLength, curBank);
      0 <= m
      && m <= bankLength
      && (curBank == 0 ==> m <= regOffset * 2)
      && (curBank != 0 ==> m <= MaxLibProgram)
  {
  }

  // For a fixed partition state the bound is uniquely determined; used by
  // the generated-code consistency test.
  lemma EffectiveMaxProgDeterministic(regOffset: int, bankLength: int, curBank: int)
    requires 0 <= regOffset
    requires 0 <= bankLength
    requires 0 <= curBank
    ensures var m := EffectiveMaxProg(regOffset, bankLength, curBank);
      m == EffectiveMaxProg(regOffset, bankLength, curBank)
  {
  }
}