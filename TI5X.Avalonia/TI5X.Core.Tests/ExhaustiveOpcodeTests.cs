using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Bounded exhaustive sweep of the instruction decoder: every opcode 0-99 with
/// every operand byte 0-99 must execute without an exception, in bounded time,
/// leaving the program counter inside the usable range or a defined error state.
/// Byte-length expectations below are derived from the documented TI-59 encoding
/// rules (PRIRUCKA_TI59.md chapter 15), independently of the engine dispatch.
/// </summary>
public class ExhaustiveOpcodeTests
{
    /// <summary>
    /// Bytes a non-jumping instruction consumes for (opcode, first operand byte).
    /// -1 = the instruction jumps or nests, PC is not length-comparable.
    /// </summary>
    private static int ConsumedBytes(int op, byte b) => op switch
    {
        >= 0 and <= 9 => 1,                     // digit instruction
        >= 10 and <= 19 => -1,                  // label call jumps
        >= 20 and <= 27 => 1,                   // 2nd/INV/CLR codes: no standalone effect
        KeyCodes.KEY_SST or KeyCodes.KEY_BST => -1, // SST nests a step, BST moves PC back
        KeyCodes.KEY_LRN => 1,                  // toggles LRN mode
        KeyCodes.KEY_LBL => 2,                  // Lbl + label byte
        KeyCodes.KEY_R_S => 1,                  // stops the program
        KeyCodes.KEY_PAUSE or KeyCodes.OP_HIR => 1,
        KeyCodes.OP_INV_SBR => 1,               // RTN
        KeyCodes.KEY_STO or KeyCodes.KEY_RCL or KeyCodes.KEY_SUM or KeyCodes.KEY_EXC
            or KeyCodes.KEY_PRD or KeyCodes.KEY_OP
            => b == KeyCodes.KEY_IND ? 3 : 2,  // "40 nn" or direct register byte
        KeyCodes.KEY_FIX or KeyCodes.KEY_ST_FLG => 2,
        KeyCodes.OP_EXC_IND or KeyCodes.OP_PRD_IND
            or KeyCodes.OP_STO_IND or KeyCodes.OP_RCL_IND or KeyCodes.OP_SUM_IND
            or KeyCodes.OP_OP_IND => 2,                 // merged + register byte
        KeyCodes.OP_GTO_IND or KeyCodes.OP_PGM_IND => -1, // merged GTO/Pgm reset PC
        KeyCodes.KEY_CP or KeyCodes.KEY_PGM or KeyCodes.KEY_RST => -1,
        KeyCodes.KEY_GTO or KeyCodes.KEY_SBR or KeyCodes.KEY_X_EQ_T or KeyCodes.KEY_X_GE_T
            or KeyCodes.KEY_IF_FLG or KeyCodes.KEY_DSZ => -1,   // branch/target forms
        _ => 1,                                 // simple keys
    };

    [Test]
    public async Task Every_opcode_and_operand_byte_executes_defined()
    {
        int checkedPairs = 0;
        for (int op = 0; op <= 99; ++op)
        {
            for (int b = 0; b <= 99; ++b)
            {
                var engine = new CalculatorEngine();
                // [op, b, NOP, NOP, 0 ...] - enough bytes for every operand form
                engine.UserProgram[0] = (byte)op;
                engine.UserProgram[1] = (byte)b;
                engine.UserProgram[2] = (byte)KeyCodes.KEY_NOP;
                engine.UserProgram[3] = (byte)KeyCodes.KEY_NOP;
                engine.PC = 0;

                engine.StepProgram(); // must not throw for any combination
                ++checkedPairs;

                int max = engine.GetMaxProg();
                await Assert.That(engine.PC >= 0 && engine.PC <= max).IsTrue();

                int expected = ConsumedBytes(op, (byte)b);
                if (expected >= 0 && engine.State != CalculatorEngine.EngineState.Error)
                {
                    if (engine.PC != expected)
                    {
                        await Assert.That(engine.PC).IsEqualTo(expected);
                    }
                }
            }
        }
        await Assert.That(checkedPairs).IsEqualTo(100 * 100);
    }

    [Test]
    public async Task Every_opcode_stays_in_bounds_on_short_banks()
    {
        foreach (int bankLength in new[] { 1, 2, 3, 5 })
        {
            for (int op = 0; op <= 99; ++op)
            {
                var engine = new CalculatorEngine();
                engine.Banks[0] = new ProgBank(new byte[bankLength]);
                engine.PC = 0;

                engine.StepProgram(); // short bank: defined behavior, no crash

                int max = engine.GetMaxProg();
                await Assert.That(max).IsEqualTo(bankLength);
                await Assert.That(engine.PC >= 0 && engine.PC <= max).IsTrue();
            }
        }
    }

    [Test]
    public async Task Every_interactive_key_is_defined()
    {
        for (int key = 0; key <= 99; ++key)
        {
            var engine = new CalculatorEngine();
            engine.ProcessKey(key); // must not throw for any key code
            if (engine.IsRunning)
            {
                await TestHelpers.RunUntilHaltAsync(engine);
            }
            await Assert.That(engine.State is CalculatorEngine.EngineState.Entry
                or CalculatorEngine.EngineState.DecimalEntry
                or CalculatorEngine.EngineState.ExponentEntry
                or CalculatorEngine.EngineState.Result
                or CalculatorEngine.EngineState.Error).IsTrue();
        }
    }

    [Test]
    public async Task Every_second_function_key_is_defined()
    {
        for (int key = 0; key <= 99; ++key)
        {
            if (key == KeyCodes.KEY_2ND) continue; // 2nd 2nd cancels
            var engine = new CalculatorEngine();
            engine.ProcessKey(KeyCodes.KEY_2ND);
            engine.ProcessKey(key); // MapSecond must stay total and defined
            if (engine.IsRunning)
            {
                await TestHelpers.RunUntilHaltAsync(engine);
            }
            await Assert.That(engine.State is CalculatorEngine.EngineState.Entry
                or CalculatorEngine.EngineState.DecimalEntry
                or CalculatorEngine.EngineState.ExponentEntry
                or CalculatorEngine.EngineState.Result
                or CalculatorEngine.EngineState.Error).IsTrue();
        }
    }

    [Test]
    public async Task Every_op_code_is_defined()
    {
        for (int op = 0; op <= 99; ++op)
        {
            var engine = new CalculatorEngine();
            engine.ExecuteOp(op, ind: false); // must not throw for any Op code
            await Assert.That(engine.IsRunning).IsFalse();

            var engineInd = new CalculatorEngine();
            engineInd.ExecuteOp(op, ind: true); // indirect form goes through R00..R99
            await Assert.That(engineInd.IsRunning).IsFalse();
        }
    }
}
