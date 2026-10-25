using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// LRN keyboard entry must produce TI-59 instruction codes: digits 00-09,
/// merged 2nd functions, merged IND codes, two-byte addresses and RTN (92).
/// </summary>
public class LrnEntryTests
{
    private static CalculatorEngine EnterLrn()
    {
        var engine = new CalculatorEngine();
        engine.ProcessKey(KeyCodes.KEY_LRN);
        return engine;
    }

    [Test]
    public async Task Digits_are_stored_as_instruction_codes()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_7); // key code 62
        engine.ProcessKey(KeyCodes.KEY_0); // key code 92
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(7);
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(0);
    }

    [Test]
    public async Task Second_function_merges_into_single_code()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_LNX); // 2nd log
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(28);
    }

    [Test]
    public async Task INV_SBR_is_stored_as_single_RTN()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_INV);
        engine.ProcessKey(KeyCodes.KEY_SBR);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(92);
    }

    [Test]
    public async Task STO_Ind_is_stored_as_merged_code()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_STO);
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_Y_TO_X); // 2nd Ind
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_5);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(72); // ST* merged code
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(5);
    }

    [Test]
    public async Task GTO_three_digit_address_is_stored_as_two_bytes()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_GTO);
        engine.ProcessKey(KeyCodes.KEY_1);
        engine.ProcessKey(KeyCodes.KEY_2);
        engine.ProcessKey(KeyCodes.KEY_3);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(61);
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(1);
        await Assert.That((int)engine.UserProgram[2]).IsEqualTo(23);
    }

    [Test]
    public async Task GTO_label_is_stored_as_label_byte()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_GTO);
        engine.ProcessKey(KeyCodes.KEY_A);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(61);
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(11);
    }

    [Test]
    public async Task Fix_operand_is_one_byte()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_LPAREN); // 2nd Fix
        engine.ProcessKey(KeyCodes.KEY_4);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(58);
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(4);
    }

    [Test]
    public async Task Dsz_register_and_three_digit_target_are_stored()
    {
        var engine = EnterLrn();
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_0); // 2nd Dsz
        engine.ProcessKey(KeyCodes.KEY_1);
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_7);
        await Assert.That((int)engine.UserProgram[0]).IsEqualTo(97);
        await Assert.That((int)engine.UserProgram[1]).IsEqualTo(1);
        await Assert.That((int)engine.UserProgram[2]).IsEqualTo(0);
        await Assert.That((int)engine.UserProgram[3]).IsEqualTo(7);
    }

    [Test]
    public async Task LRN_on_library_bank_does_not_touch_bank0()
    {
        var engine = new CalculatorEngine();
        engine.Banks[1] = new ProgBank(new byte[127]);
        engine.CurBank = 1;
        engine.PC = 0;
        engine.ProcessKey(KeyCodes.KEY_LRN);
        // STO 2nd Ind 05: the retroactive merged-code rewrite must not leak
        // into UserProgram (bank 0) while a library bank is selected.
        engine.ProcessKey(KeyCodes.KEY_STO);
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_Y_TO_X);
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_5);
        engine.ProcessKey(KeyCodes.KEY_LRN);

        for (int i = 0; i < 16; ++i)
        {
            await Assert.That((int)engine.UserProgram[i]).IsEqualTo(0);
        }
    }

    [Test]
    public async Task LRN_boundary_entry_signals_error_instead_of_clobbering()
    {
        // RegOffset 50 -> 100 usable steps (0..99).
        var engine = new CalculatorEngine();
        engine.RegOffset = 50;
        engine.PC = 99;
        engine.ProcessKey(KeyCodes.KEY_LRN);
        engine.ProcessKey(KeyCodes.KEY_7); // last step: no room to advance
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
        await Assert.That((int)engine.UserProgram[99]).IsEqualTo(0);

        // A 2-byte instruction starting at 98 stores its opcode, then the
        // operand fails with a defined error instead of overwriting the opcode.
        var engine2 = new CalculatorEngine();
        engine2.RegOffset = 50;
        engine2.PC = 98;
        engine2.ProcessKey(KeyCodes.KEY_LRN);
        engine2.ProcessKey(KeyCodes.KEY_STO); // opcode at 98, PC -> 99
        engine2.ProcessKey(KeyCodes.KEY_0);
        engine2.ProcessKey(KeyCodes.KEY_5);    // operand at 99: no room -> error
        await Assert.That((int)engine2.UserProgram[98]).IsEqualTo(42);
        await Assert.That((int)engine2.UserProgram[99]).IsEqualTo(0);
        await Assert.That(engine2.State == CalculatorEngine.EngineState.Error).IsTrue();

        // Control: the same instruction one step earlier fits completely.
        var engine3 = new CalculatorEngine();
        engine3.RegOffset = 50;
        engine3.PC = 97;
        engine3.ProcessKey(KeyCodes.KEY_LRN);
        engine3.ProcessKey(KeyCodes.KEY_STO);
        engine3.ProcessKey(KeyCodes.KEY_0);
        engine3.ProcessKey(KeyCodes.KEY_5);
        await Assert.That((int)engine3.UserProgram[97]).IsEqualTo(42);
        await Assert.That((int)engine3.UserProgram[98]).IsEqualTo(5);
        await Assert.That(engine3.PC).IsEqualTo(99);
        await Assert.That(engine3.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Full_program_entered_in_LRN_runs()
    {
        var engine = EnterLrn();
        // 2nd Lbl, A, 7, STO 01, INV SBR
        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_SBR); // 2nd Lbl
        engine.ProcessKey(KeyCodes.KEY_A);
        engine.ProcessKey(KeyCodes.KEY_7);
        engine.ProcessKey(KeyCodes.KEY_STO);
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_1);
        engine.ProcessKey(KeyCodes.KEY_INV);
        engine.ProcessKey(KeyCodes.KEY_SBR);
        engine.ProcessKey(KeyCodes.KEY_LRN); // leave LRN mode

        int[] expected = { 76, 11, 07, 42, 01, 92 };
        for (int i = 0; i < expected.Length; ++i)
        {
            await Assert.That((int)engine.UserProgram[i]).IsEqualTo(expected[i]);
        }

        engine.ProcessKey(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[1]).IsEqualTo(7.0).Within(1e-9);
    }
}
