// Dafny program the_program compiled into C#
// To recompile, you will need the libraries
//     System.Runtime.Numerics.dll System.Collections.Immutable.dll
// but the 'dotnet' tool in .NET should pick those up automatically.
// Optionally, you may want to include compiler switches like
//     /debug /nowarn:162,164,168,183,219,436,1717,1718

using System;
using System.Numerics;
using System.Collections;

namespace TI59__Nibbles {

  public partial class __default {
    public static Dafny.ISequence<byte> ZeroReg() {
      return ((System.Func<Dafny.ISequence<byte>>) (() => {
        BigInteger dim0 = new BigInteger(16);
        var arr0 = new byte[Dafny.Helpers.ToIntChecked(dim0, "array size exceeds memory limit")];
        for (int i0 = 0; i0 < dim0; i0++) {
          var _0___v1 = (BigInteger) i0;
          arr0[(int)(_0___v1)] = (byte)(0);
        }
        return Dafny.Sequence<byte>.FromArray(arr0);
      }))();
    }
    public static Dafny.ISequence<byte> RegUpd(Dafny.ISequence<byte> s, BigInteger i, byte v)
    {
      return Dafny.Sequence<byte>.Update(s, i, v);
    }
    public static bool BcdValid(Dafny.ISequence<byte> s) {
      return Dafny.Helpers.Id<Func<Dafny.ISequence<byte>, bool>>((_0_s) => Dafny.Helpers.Quantifier<BigInteger>(Dafny.Helpers.IntegerRange(BigInteger.Zero, new BigInteger(16)), true, (((_forall_var_0) => {
        BigInteger _1_i = (BigInteger)_forall_var_0;
        return !(((_1_i).Sign != -1) && ((_1_i) < (new BigInteger(16)))) || ((new BigInteger((_0_s).Select(_1_i))) < (new BigInteger(10)));
      }))))(s);
    }
    public static TI59__Nibbles._IMask MaskOf(BigInteger idx) {
      if ((idx) == (BigInteger.One)) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
      } else if ((idx) == (new BigInteger(2))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(0));
      } else if ((idx) == (new BigInteger(3))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(1));
      } else if ((idx) == (new BigInteger(4))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(12));
      } else if ((idx) == (new BigInteger(5))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(3), new BigInteger(3), (byte)(1));
      } else if ((idx) == (new BigInteger(6))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(2), BigInteger.One, (byte)(0));
      } else if ((idx) == (new BigInteger(7))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(2), BigInteger.One, (byte)(1));
      } else if ((idx) == (new BigInteger(9))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(15), new BigInteger(3), (byte)(0));
      } else if ((idx) == (new BigInteger(11))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(15), new BigInteger(3), (byte)(5));
      } else if ((idx) == (new BigInteger(12))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), BigInteger.One, (byte)(0));
      } else if ((idx) == (new BigInteger(13))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), new BigInteger(3), (byte)(1));
      } else if ((idx) == (new BigInteger(14))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), new BigInteger(15), (byte)(1));
      } else if ((idx) == (new BigInteger(15))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), BigInteger.One, (byte)(1));
      } else {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
      }
    }
    public static byte Bit8(BigInteger b) {
      if ((b).Sign == 0) {
        return (byte)(1);
      } else {
        return unchecked((byte)(((byte)((TI59__Nibbles.__default.Bit8((b) - (BigInteger.One))) << ((int)((byte)(1)))))));
      }
    }
    public static ushort Bit16(BigInteger b) {
      if ((b).Sign == 0) {
        return (ushort)(1);
      } else {
        return unchecked((ushort)(((ushort)((TI59__Nibbles.__default.Bit16((b) - (BigInteger.One))) << ((int)((byte)(1)))))));
      }
    }
    public static Dafny.Rune Nul() {
      return new Dafny.Rune((int)(BigInteger.Zero));
    }
    public static ushort FLG__IDLE { get {
      return (ushort)(1);
    } }
    public static ushort FLG__HOLD { get {
      return (ushort)(2);
    } }
    public static ushort FLG__JUMP { get {
      return (ushort)(4);
    } }
    public static ushort FLG__IO__VALID { get {
      return (ushort)(1024);
    } }
    public static ushort FLG__COND { get {
      return (ushort)(2048);
    } }
    public static ushort FLG__COND__LAST { get {
      return (ushort)(4096);
    } }
    public static ushort FLG__BUSY { get {
      return (ushort)(32768);
    } }
    public static ushort EXT__PREG { get {
      return (ushort)(1);
    } }
    public static ushort EXT__COND { get {
      return (ushort)(2);
    } }
    public static ushort EXT__HOLD { get {
      return (ushort)(4);
    } }
  }

  public partial class Reg {
    private static readonly Dafny.ISequence<byte> Witness = ((System.Func<Dafny.ISequence<byte>>) (() => {
    BigInteger dim1 = new BigInteger(16);
    var arr1 = new byte[Dafny.Helpers.ToIntChecked(dim1, "array size exceeds memory limit")];
    for (int i1 = 0; i1 < dim1; i1++) {
      var _0___v0 = (BigInteger) i1;
      arr1[(int)(_0___v0)] = (byte)(0);
    }
    return Dafny.Sequence<byte>.FromArray(arr1);
  }))();
    public static Dafny.ISequence<byte> Default() {
      return Witness;
    }
    private static readonly Dafny.TypeDescriptor<Dafny.ISequence<byte>> _TYPE = new Dafny.TypeDescriptor<Dafny.ISequence<byte>>(TI59__Nibbles.Reg.Default());
    public static Dafny.TypeDescriptor<Dafny.ISequence<byte>> _TypeDescriptor() {
      return _TYPE;
    }
    public static bool _Is(Dafny.ISequence<byte> __source) {
      Dafny.ISequence<byte> _1_s = __source;
      return (new BigInteger((_1_s).Count)) == (new BigInteger(16));
    }
  }

  public interface _IMask {
    bool is_Mask { get; }
    BigInteger dtor_start { get; }
    BigInteger dtor_end { get; }
    BigInteger dtor_cpos { get; }
    byte dtor_cval { get; }
    _IMask DowncastClone();
  }
  public class Mask : _IMask {
    public readonly BigInteger _start;
    public readonly BigInteger _end;
    public readonly BigInteger _cpos;
    public readonly byte _cval;
    public Mask(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      this._start = start;
      this._end = end;
      this._cpos = cpos;
      this._cval = cval;
    }
    public _IMask DowncastClone() {
      if (this is _IMask dt) { return dt; }
      return new Mask(_start, _end, _cpos, _cval);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Nibbles.Mask;
      return oth != null && this._start == oth._start && this._end == oth._end && this._cpos == oth._cpos && this._cval == oth._cval;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._start));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._end));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cpos));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cval));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Nibbles.Mask.Mask";
      s += "(";
      s += Dafny.Helpers.ToString(this._start);
      s += ", ";
      s += Dafny.Helpers.ToString(this._end);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cpos);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cval);
      s += ")";
      return s;
    }
    private static readonly TI59__Nibbles._IMask theDefault = create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, 0);
    public static TI59__Nibbles._IMask Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TYPE = new Dafny.TypeDescriptor<TI59__Nibbles._IMask>(TI59__Nibbles.Mask.Default());
    public static Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IMask create(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      return new Mask(start, end, cpos, cval);
    }
    public static _IMask create_Mask(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      return create(start, end, cpos, cval);
    }
    public bool is_Mask { get { return true; } }
    public BigInteger dtor_start {
      get {
        return this._start;
      }
    }
    public BigInteger dtor_end {
      get {
        return this._end;
      }
    }
    public BigInteger dtor_cpos {
      get {
        return this._cpos;
      }
    }
    public byte dtor_cval {
      get {
        return this._cval;
      }
    }
  }

  public partial class AluMask {
    private static readonly TI59__Nibbles._IMask Witness = TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
    public static TI59__Nibbles._IMask Default() {
      return Witness;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TYPE = new Dafny.TypeDescriptor<TI59__Nibbles._IMask>(TI59__Nibbles.AluMask.Default());
    public static Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TypeDescriptor() {
      return _TYPE;
    }
    public static bool _Is(TI59__Nibbles._IMask __source) {
      TI59__Nibbles._IMask _2_m = __source;
      return (((((_2_m).dtor_start).Sign != -1) && (((_2_m).dtor_start) <= (new BigInteger(15)))) && (((_2_m).dtor_start) <= (((_2_m).dtor_end) + (BigInteger.One)))) && (((_2_m).dtor_end) <= (new BigInteger(15)));
    }
  }
} // end of namespace TI59__Nibbles
namespace TI59__Bus {

  public partial class __default {
    public static TI59__Bus._IBus BusInit() {
      return TI59__Bus.Bus.create((ushort)(0), (ushort)(0), TI59__Nibbles.__default.ZeroReg(), new Dafny.Rune(' '), false, false, (byte)(0), new BigInteger(15), false, BigInteger.Zero, BigInteger.Zero);
    }
    public static TI59__Bus._IBus BusCycleStart(TI59__Bus._IBus b) {
      TI59__Bus._IBus _0_dt__update__tmp_h0 = b;
      BigInteger _1_dt__update_hcycleCount_h0 = ((b).dtor_cycleCount) + (BigInteger.One);
      BigInteger _2_dt__update_haddr_h0 = new BigInteger(-1);
      Dafny.ISequence<byte> _3_dt__update_hio_h0 = TI59__Nibbles.__default.ZeroReg();
      ushort _4_dt__update_hirg_h0 = (ushort)(0);
      ushort _5_dt__update_hext_h0 = (ushort)(0);
      return TI59__Bus.Bus.create(_5_dt__update_hext_h0, _4_dt__update_hirg_h0, _3_dt__update_hio_h0, (_0_dt__update__tmp_h0).dtor_displayDigit, (_0_dt__update__tmp_h0).dtor_displayDpt, (_0_dt__update__tmp_h0).dtor_displaySegH, (_0_dt__update__tmp_h0).dtor_keyLine, (_0_dt__update__tmp_h0).dtor_dstate, (_0_dt__update__tmp_h0).dtor_idle, _2_dt__update_haddr_h0, _1_dt__update_hcycleCount_h0);
    }
    public static TI59__Bus._IBus BusAfterS14(TI59__Bus._IBus b) {
      TI59__Bus._IBus _0_dt__update__tmp_h0 = b;
      BigInteger _1_dt__update_hdstate_h0 = ((((b).dtor_dstate).Sign != 0) ? (((b).dtor_dstate) - (BigInteger.One)) : (new BigInteger(15)));
      byte _2_dt__update_hkeyLine_h0 = (byte)(0);
      return TI59__Bus.Bus.create((_0_dt__update__tmp_h0).dtor_ext, (_0_dt__update__tmp_h0).dtor_irg, (_0_dt__update__tmp_h0).dtor_io, (_0_dt__update__tmp_h0).dtor_displayDigit, (_0_dt__update__tmp_h0).dtor_displayDpt, (_0_dt__update__tmp_h0).dtor_displaySegH, _2_dt__update_hkeyLine_h0, _1_dt__update_hdstate_h0, (_0_dt__update__tmp_h0).dtor_idle, (_0_dt__update__tmp_h0).dtor_addr, (_0_dt__update__tmp_h0).dtor_cycleCount);
    }
  }

  public interface _IBus {
    bool is_Bus { get; }
    ushort dtor_ext { get; }
    ushort dtor_irg { get; }
    Dafny.ISequence<byte> dtor_io { get; }
    Dafny.Rune dtor_displayDigit { get; }
    bool dtor_displayDpt { get; }
    bool dtor_displaySegH { get; }
    byte dtor_keyLine { get; }
    BigInteger dtor_dstate { get; }
    bool dtor_idle { get; }
    BigInteger dtor_addr { get; }
    BigInteger dtor_cycleCount { get; }
    _IBus DowncastClone();
  }
  public class Bus : _IBus {
    public readonly ushort _ext;
    public readonly ushort _irg;
    public readonly Dafny.ISequence<byte> _io;
    public readonly Dafny.Rune _displayDigit;
    public readonly bool _displayDpt;
    public readonly bool _displaySegH;
    public readonly byte _keyLine;
    public readonly BigInteger _dstate;
    public readonly bool _idle;
    public readonly BigInteger _addr;
    public readonly BigInteger _cycleCount;
    public Bus(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      this._ext = ext;
      this._irg = irg;
      this._io = io;
      this._displayDigit = displayDigit;
      this._displayDpt = displayDpt;
      this._displaySegH = displaySegH;
      this._keyLine = keyLine;
      this._dstate = dstate;
      this._idle = idle;
      this._addr = addr;
      this._cycleCount = cycleCount;
    }
    public _IBus DowncastClone() {
      if (this is _IBus dt) { return dt; }
      return new Bus(_ext, _irg, _io, _displayDigit, _displayDpt, _displaySegH, _keyLine, _dstate, _idle, _addr, _cycleCount);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Bus.Bus;
      return oth != null && this._ext == oth._ext && this._irg == oth._irg && object.Equals(this._io, oth._io) && this._displayDigit == oth._displayDigit && this._displayDpt == oth._displayDpt && this._displaySegH == oth._displaySegH && this._keyLine == oth._keyLine && this._dstate == oth._dstate && this._idle == oth._idle && this._addr == oth._addr && this._cycleCount == oth._cycleCount;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._ext));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._irg));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._io));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displayDigit));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displayDpt));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displaySegH));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyLine));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._dstate));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._idle));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._addr));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cycleCount));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Bus.Bus.Bus";
      s += "(";
      s += Dafny.Helpers.ToString(this._ext);
      s += ", ";
      s += Dafny.Helpers.ToString(this._irg);
      s += ", ";
      s += Dafny.Helpers.ToString(this._io);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displayDigit);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displayDpt);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displaySegH);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyLine);
      s += ", ";
      s += Dafny.Helpers.ToString(this._dstate);
      s += ", ";
      s += Dafny.Helpers.ToString(this._idle);
      s += ", ";
      s += Dafny.Helpers.ToString(this._addr);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cycleCount);
      s += ")";
      return s;
    }
    private static readonly TI59__Bus._IBus theDefault = create(0, 0, TI59__Nibbles.Reg.Default(), new Dafny.Rune('D'), false, false, 0, BigInteger.Zero, false, BigInteger.Zero, BigInteger.Zero);
    public static TI59__Bus._IBus Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Bus._IBus> _TYPE = new Dafny.TypeDescriptor<TI59__Bus._IBus>(TI59__Bus.Bus.Default());
    public static Dafny.TypeDescriptor<TI59__Bus._IBus> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IBus create(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      return new Bus(ext, irg, io, displayDigit, displayDpt, displaySegH, keyLine, dstate, idle, addr, cycleCount);
    }
    public static _IBus create_Bus(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      return create(ext, irg, io, displayDigit, displayDpt, displaySegH, keyLine, dstate, idle, addr, cycleCount);
    }
    public bool is_Bus { get { return true; } }
    public ushort dtor_ext {
      get {
        return this._ext;
      }
    }
    public ushort dtor_irg {
      get {
        return this._irg;
      }
    }
    public Dafny.ISequence<byte> dtor_io {
      get {
        return this._io;
      }
    }
    public Dafny.Rune dtor_displayDigit {
      get {
        return this._displayDigit;
      }
    }
    public bool dtor_displayDpt {
      get {
        return this._displayDpt;
      }
    }
    public bool dtor_displaySegH {
      get {
        return this._displaySegH;
      }
    }
    public byte dtor_keyLine {
      get {
        return this._keyLine;
      }
    }
    public BigInteger dtor_dstate {
      get {
        return this._dstate;
      }
    }
    public bool dtor_idle {
      get {
        return this._idle;
      }
    }
    public BigInteger dtor_addr {
      get {
        return this._addr;
      }
    }
    public BigInteger dtor_cycleCount {
      get {
        return this._cycleCount;
      }
    }
  }
} // end of namespace TI59__Bus
namespace TI59__Events {


  public interface _ITi59Event {
    bool is_EvPrintChar { get; }
    bool is_EvPrintFunc { get; }
    bool is_EvPrintNotFound { get; }
    bool is_EvPrintClear { get; }
    bool is_EvPrint { get; }
    bool is_EvCrdRd { get; }
    bool is_EvCrdWr { get; }
    bool is_EvCrdOff { get; }
    BigInteger dtor_head { get; }
    Dafny.Rune dtor_ch { get; }
    Dafny.ISequence<Dafny.Rune> dtor_s { get; }
    BigInteger dtor_code { get; }
    Dafny.ISequence<Dafny.Rune> dtor_buf { get; }
    BigInteger dtor_pc { get; }
    byte dtor_data { get; }
    _ITi59Event DowncastClone();
  }
  public abstract class Ti59Event : _ITi59Event {
    public Ti59Event() {
    }
    private static readonly TI59__Events._ITi59Event theDefault = create_EvPrintChar(BigInteger.Zero, new Dafny.Rune('D'));
    public static TI59__Events._ITi59Event Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Events._ITi59Event> _TYPE = new Dafny.TypeDescriptor<TI59__Events._ITi59Event>(TI59__Events.Ti59Event.Default());
    public static Dafny.TypeDescriptor<TI59__Events._ITi59Event> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ITi59Event create_EvPrintChar(BigInteger head, Dafny.Rune ch) {
      return new Ti59Event_EvPrintChar(head, ch);
    }
    public static _ITi59Event create_EvPrintFunc(BigInteger head, Dafny.ISequence<Dafny.Rune> s) {
      return new Ti59Event_EvPrintFunc(head, s);
    }
    public static _ITi59Event create_EvPrintNotFound(BigInteger head, BigInteger code) {
      return new Ti59Event_EvPrintNotFound(head, code);
    }
    public static _ITi59Event create_EvPrintClear(BigInteger head, Dafny.ISequence<Dafny.Rune> buf) {
      return new Ti59Event_EvPrintClear(head, buf);
    }
    public static _ITi59Event create_EvPrint(BigInteger head, Dafny.ISequence<Dafny.Rune> buf) {
      return new Ti59Event_EvPrint(head, buf);
    }
    public static _ITi59Event create_EvCrdRd(BigInteger pc, byte data) {
      return new Ti59Event_EvCrdRd(pc, data);
    }
    public static _ITi59Event create_EvCrdWr(BigInteger pc, byte data) {
      return new Ti59Event_EvCrdWr(pc, data);
    }
    public static _ITi59Event create_EvCrdOff(BigInteger pc) {
      return new Ti59Event_EvCrdOff(pc);
    }
    public bool is_EvPrintChar { get { return this is Ti59Event_EvPrintChar; } }
    public bool is_EvPrintFunc { get { return this is Ti59Event_EvPrintFunc; } }
    public bool is_EvPrintNotFound { get { return this is Ti59Event_EvPrintNotFound; } }
    public bool is_EvPrintClear { get { return this is Ti59Event_EvPrintClear; } }
    public bool is_EvPrint { get { return this is Ti59Event_EvPrint; } }
    public bool is_EvCrdRd { get { return this is Ti59Event_EvCrdRd; } }
    public bool is_EvCrdWr { get { return this is Ti59Event_EvCrdWr; } }
    public bool is_EvCrdOff { get { return this is Ti59Event_EvCrdOff; } }
    public BigInteger dtor_head {
      get {
        var d = this;
        if (d is Ti59Event_EvPrintChar) { return ((Ti59Event_EvPrintChar)d)._head; }
        if (d is Ti59Event_EvPrintFunc) { return ((Ti59Event_EvPrintFunc)d)._head; }
        if (d is Ti59Event_EvPrintNotFound) { return ((Ti59Event_EvPrintNotFound)d)._head; }
        if (d is Ti59Event_EvPrintClear) { return ((Ti59Event_EvPrintClear)d)._head; }
        return ((Ti59Event_EvPrint)d)._head;
      }
    }
    public Dafny.Rune dtor_ch {
      get {
        var d = this;
        return ((Ti59Event_EvPrintChar)d)._ch;
      }
    }
    public Dafny.ISequence<Dafny.Rune> dtor_s {
      get {
        var d = this;
        return ((Ti59Event_EvPrintFunc)d)._s;
      }
    }
    public BigInteger dtor_code {
      get {
        var d = this;
        return ((Ti59Event_EvPrintNotFound)d)._code;
      }
    }
    public Dafny.ISequence<Dafny.Rune> dtor_buf {
      get {
        var d = this;
        if (d is Ti59Event_EvPrintClear) { return ((Ti59Event_EvPrintClear)d)._buf; }
        return ((Ti59Event_EvPrint)d)._buf;
      }
    }
    public BigInteger dtor_pc {
      get {
        var d = this;
        if (d is Ti59Event_EvCrdRd) { return ((Ti59Event_EvCrdRd)d)._pc; }
        if (d is Ti59Event_EvCrdWr) { return ((Ti59Event_EvCrdWr)d)._pc; }
        return ((Ti59Event_EvCrdOff)d)._pc;
      }
    }
    public byte dtor_data {
      get {
        var d = this;
        if (d is Ti59Event_EvCrdRd) { return ((Ti59Event_EvCrdRd)d)._data; }
        return ((Ti59Event_EvCrdWr)d)._data;
      }
    }
    public abstract _ITi59Event DowncastClone();
  }
  public class Ti59Event_EvPrintChar : Ti59Event {
    public readonly BigInteger _head;
    public readonly Dafny.Rune _ch;
    public Ti59Event_EvPrintChar(BigInteger head, Dafny.Rune ch) : base() {
      this._head = head;
      this._ch = ch;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvPrintChar(_head, _ch);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvPrintChar;
      return oth != null && this._head == oth._head && this._ch == oth._ch;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._ch));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvPrintChar";
      s += "(";
      s += Dafny.Helpers.ToString(this._head);
      s += ", ";
      s += Dafny.Helpers.ToString(this._ch);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvPrintFunc : Ti59Event {
    public readonly BigInteger _head;
    public readonly Dafny.ISequence<Dafny.Rune> _s;
    public Ti59Event_EvPrintFunc(BigInteger head, Dafny.ISequence<Dafny.Rune> s) : base() {
      this._head = head;
      this._s = s;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvPrintFunc(_head, _s);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvPrintFunc;
      return oth != null && this._head == oth._head && object.Equals(this._s, oth._s);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._s));
      return (int) hash;
    }
    public override string ToString() {
      string ss = "TI59_Events.Ti59Event.EvPrintFunc";
      ss += "(";
      ss += Dafny.Helpers.ToString(this._head);
      ss += ", ";
      ss += this._s.ToVerbatimString(true);
      ss += ")";
      return ss;
    }
  }
  public class Ti59Event_EvPrintNotFound : Ti59Event {
    public readonly BigInteger _head;
    public readonly BigInteger _code;
    public Ti59Event_EvPrintNotFound(BigInteger head, BigInteger code) : base() {
      this._head = head;
      this._code = code;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvPrintNotFound(_head, _code);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvPrintNotFound;
      return oth != null && this._head == oth._head && this._code == oth._code;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 2;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._code));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvPrintNotFound";
      s += "(";
      s += Dafny.Helpers.ToString(this._head);
      s += ", ";
      s += Dafny.Helpers.ToString(this._code);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvPrintClear : Ti59Event {
    public readonly BigInteger _head;
    public readonly Dafny.ISequence<Dafny.Rune> _buf;
    public Ti59Event_EvPrintClear(BigInteger head, Dafny.ISequence<Dafny.Rune> buf) : base() {
      this._head = head;
      this._buf = buf;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvPrintClear(_head, _buf);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvPrintClear;
      return oth != null && this._head == oth._head && object.Equals(this._buf, oth._buf);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 3;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._buf));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvPrintClear";
      s += "(";
      s += Dafny.Helpers.ToString(this._head);
      s += ", ";
      s += this._buf.ToVerbatimString(true);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvPrint : Ti59Event {
    public readonly BigInteger _head;
    public readonly Dafny.ISequence<Dafny.Rune> _buf;
    public Ti59Event_EvPrint(BigInteger head, Dafny.ISequence<Dafny.Rune> buf) : base() {
      this._head = head;
      this._buf = buf;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvPrint(_head, _buf);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvPrint;
      return oth != null && this._head == oth._head && object.Equals(this._buf, oth._buf);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 4;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._buf));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvPrint";
      s += "(";
      s += Dafny.Helpers.ToString(this._head);
      s += ", ";
      s += this._buf.ToVerbatimString(true);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvCrdRd : Ti59Event {
    public readonly BigInteger _pc;
    public readonly byte _data;
    public Ti59Event_EvCrdRd(BigInteger pc, byte data) : base() {
      this._pc = pc;
      this._data = data;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvCrdRd(_pc, _data);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvCrdRd;
      return oth != null && this._pc == oth._pc && this._data == oth._data;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 5;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pc));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._data));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvCrdRd";
      s += "(";
      s += Dafny.Helpers.ToString(this._pc);
      s += ", ";
      s += Dafny.Helpers.ToString(this._data);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvCrdWr : Ti59Event {
    public readonly BigInteger _pc;
    public readonly byte _data;
    public Ti59Event_EvCrdWr(BigInteger pc, byte data) : base() {
      this._pc = pc;
      this._data = data;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvCrdWr(_pc, _data);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvCrdWr;
      return oth != null && this._pc == oth._pc && this._data == oth._data;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 6;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pc));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._data));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvCrdWr";
      s += "(";
      s += Dafny.Helpers.ToString(this._pc);
      s += ", ";
      s += Dafny.Helpers.ToString(this._data);
      s += ")";
      return s;
    }
  }
  public class Ti59Event_EvCrdOff : Ti59Event {
    public readonly BigInteger _pc;
    public Ti59Event_EvCrdOff(BigInteger pc) : base() {
      this._pc = pc;
    }
    public override _ITi59Event DowncastClone() {
      if (this is _ITi59Event dt) { return dt; }
      return new Ti59Event_EvCrdOff(_pc);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Events.Ti59Event_EvCrdOff;
      return oth != null && this._pc == oth._pc;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 7;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pc));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Events.Ti59Event.EvCrdOff";
      s += "(";
      s += Dafny.Helpers.ToString(this._pc);
      s += ")";
      return s;
    }
  }
} // end of namespace TI59__Events
namespace TI59__Print {

  public partial class __default {
    public static bool PrintWF(TI59__Print._IPrintState p) {
      return ((new BigInteger(((p).dtor_buffer).Count)) == (new BigInteger(20))) && ((((p).dtor_head).Sign != -1) && (((p).dtor_head) <= (new BigInteger(19))));
    }
    public static TI59__Print._IPrintState PrintInit() {
      return TI59__Print.PrintState.create(((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim2 = new BigInteger(20);
  var arr2 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim2, "array size exceeds memory limit")];
  for (int i2 = 0; i2 < dim2; i2++) {
    var _0___v0 = (BigInteger) i2;
    arr2[(int)(_0___v0)] = new Dafny.Rune('X');
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr2);
}))(), new BigInteger(5), Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.FromElements());
    }
    public static BigInteger PrintHeadStep(BigInteger head) {
      if ((head).Sign == 0) {
        return new BigInteger(19);
      } else {
        return (head) - (BigInteger.One);
      }
    }
    public static Dafny.ISequence<Dafny.Rune> PrintFont() {
      return Dafny.Sequence<Dafny.Rune>.FromElements(new Dafny.Rune(' '), new Dafny.Rune('0'), new Dafny.Rune('1'), new Dafny.Rune('2'), new Dafny.Rune('3'), new Dafny.Rune('4'), new Dafny.Rune('5'), new Dafny.Rune('6'), new Dafny.Rune('7'), new Dafny.Rune('8'), new Dafny.Rune('9'), new Dafny.Rune('A'), new Dafny.Rune('B'), new Dafny.Rune('C'), new Dafny.Rune('D'), new Dafny.Rune('E'), new Dafny.Rune('-'), new Dafny.Rune('F'), new Dafny.Rune('G'), new Dafny.Rune('H'), new Dafny.Rune('I'), new Dafny.Rune('J'), new Dafny.Rune('K'), new Dafny.Rune('L'), new Dafny.Rune('M'), new Dafny.Rune('N'), new Dafny.Rune('O'), new Dafny.Rune('P'), new Dafny.Rune('Q'), new Dafny.Rune('R'), new Dafny.Rune('S'), new Dafny.Rune('T'), new Dafny.Rune('.'), new Dafny.Rune('U'), new Dafny.Rune('V'), new Dafny.Rune('W'), new Dafny.Rune('X'), new Dafny.Rune('Y'), new Dafny.Rune('Z'), new Dafny.Rune('+'), new Dafny.Rune('x'), new Dafny.Rune('*'), new Dafny.Rune('s'), new Dafny.Rune('p'), new Dafny.Rune('e'), new Dafny.Rune('('), new Dafny.Rune(')'), new Dafny.Rune(','), new Dafny.Rune('^'), new Dafny.Rune('%'), new Dafny.Rune('|'), new Dafny.Rune('/'), new Dafny.Rune('='), new Dafny.Rune('\''), new Dafny.Rune('#'), new Dafny.Rune('~'), new Dafny.Rune('z'), new Dafny.Rune('?'), new Dafny.Rune(':'), new Dafny.Rune('!'), new Dafny.Rune(']'), new Dafny.Rune('"'), new Dafny.Rune('['), new Dafny.Rune('$'));
    }
    public static Dafny.ISequence<Dafny.Rune> PrintFontSr60() {
      return Dafny.Sequence<Dafny.Rune>.FromElements(new Dafny.Rune(' '), new Dafny.Rune('0'), new Dafny.Rune('1'), new Dafny.Rune('2'), new Dafny.Rune('3'), new Dafny.Rune('4'), new Dafny.Rune('5'), new Dafny.Rune('6'), new Dafny.Rune('7'), new Dafny.Rune('8'), new Dafny.Rune('9'), new Dafny.Rune('.'), new Dafny.Rune('z'), new Dafny.Rune('#'), new Dafny.Rune('!'), new Dafny.Rune('"'), new Dafny.Rune('='), new Dafny.Rune('-'), new Dafny.Rune('+'), new Dafny.Rune(':'), new Dafny.Rune('x'), new Dafny.Rune('\''), new Dafny.Rune('~'), new Dafny.Rune(')'), new Dafny.Rune('s'), new Dafny.Rune('^'), new Dafny.Rune('$'), new Dafny.Rune('['), new Dafny.Rune('/'), new Dafny.Rune('|'), new Dafny.Rune('e'), new Dafny.Rune(']'), new Dafny.Rune('Q'), new Dafny.Rune('R'), new Dafny.Rune('S'), new Dafny.Rune('T'), new Dafny.Rune('U'), new Dafny.Rune('V'), new Dafny.Rune('W'), new Dafny.Rune('X'), new Dafny.Rune('Y'), new Dafny.Rune('Z'), new Dafny.Rune('*'), new Dafny.Rune('p'), new Dafny.Rune('?'), new Dafny.Rune(','), new Dafny.Rune('%'), new Dafny.Rune('('), new Dafny.Rune('A'), new Dafny.Rune('B'), new Dafny.Rune('C'), new Dafny.Rune('D'), new Dafny.Rune('E'), new Dafny.Rune('F'), new Dafny.Rune('G'), new Dafny.Rune('H'), new Dafny.Rune('I'), new Dafny.Rune('J'), new Dafny.Rune('K'), new Dafny.Rune('L'), new Dafny.Rune('M'), new Dafny.Rune('N'), new Dafny.Rune('O'), new Dafny.Rune('P'));
    }
    public static Dafny.ISequence<_System._ITuple2<byte, Dafny.ISequence<Dafny.Rune>>> PrintFuncTable() {
      return Dafny.Sequence<_System._ITuple2<byte, Dafny.ISequence<Dafny.Rune>>>.FromElements(_System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(0), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("   ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(17), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" = ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(18), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" - ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(19), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" + ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(22), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" / ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(23), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" x ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(26), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("xsY")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(27), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("Y^x")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(33), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("CLR")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(34), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("INV")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(35), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("DPT")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(38), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("CE ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(39), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("+/-")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(45), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("EE ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(49), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("e^x")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(51), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("x^2")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(54), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("1/x")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(60), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("sX ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(61), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("X|Y")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(81), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("LNX")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(83), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("PRM")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(84), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" % ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(86), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("COS")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(87), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("SIN")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(93), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("TAN")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(97), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("SUM")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(102), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("STO")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(103), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("pi ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(104), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("RCL")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(105), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("$+ ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(112), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("ERR")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(113), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" ( ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(114), Dafny.Sequence<Dafny.Rune>.UnicodeFromString(" ) ")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(115), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("LRN")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(116), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("RUN")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(118), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("HLT")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(120), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("STP")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(122), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("GTO")), _System.Tuple2<byte, Dafny.ISequence<Dafny.Rune>>.create((byte)(124), Dafny.Sequence<Dafny.Rune>.UnicodeFromString("IF ")));
    }
    public static TI59__Print._IFuncIdx FuncLookup(Dafny.ISequence<_System._ITuple2<byte, Dafny.ISequence<Dafny.Rune>>> t, byte code, BigInteger k)
    {
    TAIL_CALL_START: ;
      if ((k) >= (new BigInteger((t).Count))) {
        return TI59__Print.FuncIdx.create_FNone();
      } else if ((((t).Select(k)).dtor__0) == (code)) {
        return TI59__Print.FuncIdx.create_FFound(k);
      } else {
        Dafny.ISequence<_System._ITuple2<byte, Dafny.ISequence<Dafny.Rune>>> _in0 = t;
        byte _in1 = code;
        BigInteger _in2 = (k) + (BigInteger.One);
        t = _in0;
        code = _in1;
        k = _in2;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Print._IFuncIdx FuncMatch(byte code) {
      return TI59__Print.__default.FuncLookup(TI59__Print.__default.PrintFuncTable(), code, BigInteger.Zero);
    }
    public static Dafny.ISequence<Dafny.Rune> BufStr20(Dafny.ISequence<Dafny.Rune> buffer) {
      return (buffer).Subsequence(BigInteger.Zero, new BigInteger(20));
    }
    public static _System._ITuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>> PrintStep(TI59__Print._IPrintState p, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = p;
      var _pat_let_tv1 = p;
      var _pat_let_tv2 = p;
      var _pat_let_tv3 = p;
      var _pat_let_tv4 = p;
      var _pat_let_tv5 = p;
      var _pat_let_tv6 = p;
      if ((((sstate) != (new BigInteger(15))) || (write)) || (((ushort)(((bus).dtor_irg) & ((ushort)(65295)))) != ((ushort)(2568)))) {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(p, Dafny.Sequence<TI59__Events._ITi59Event>.FromElements());
      } else if (((bus).dtor_irg) == ((ushort)(2664))) {
        BigInteger _0_code = new BigInteger((ushort)(((ushort)(((bus).dtor_ext) >> ((int)((byte)(3))))) & ((ushort)(63))));
        Dafny.Rune _1_ch = (TI59__Print.__default.PrintFont()).Select(_0_code);
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let0_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let0_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(TI59__Print.__default.PrintHeadStep((_pat_let_tv0).dtor_head), _pat_let1_0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_pat_let1_0, _3_dt__update_hhead_h0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(Dafny.Sequence<Dafny.Rune>.Update((_pat_let_tv1).dtor_buffer, (_pat_let_tv2).dtor_head, _1_ch), _pat_let2_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_pat_let2_0, _4_dt__update_hbuffer_h0 => TI59__Print.PrintState.create(_4_dt__update_hbuffer_h0, _3_dt__update_hhead_h0, (_2_dt__update__tmp_h0).dtor_tape))))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements(TI59__Events.Ti59Event.create_EvPrintChar((p).dtor_head, _1_ch)));
      } else if (((bus).dtor_irg) == ((ushort)(2680))) {
        BigInteger _5_code = new BigInteger((ushort)(((ushort)(((bus).dtor_ext) >> ((int)((byte)(3))))) & ((ushort)(127))));
        TI59__Print._IFuncIdx _source0 = TI59__Print.__default.FuncMatch((byte)(_5_code));
        {
          if (_source0.is_FNone) {
            BigInteger _6_h0 = (p).dtor_head;
            BigInteger _7_h1 = TI59__Print.__default.PrintHeadStep(_6_h0);
            BigInteger _8_h2 = TI59__Print.__default.PrintHeadStep(_7_h1);
            BigInteger _9_h3 = TI59__Print.__default.PrintHeadStep(_8_h2);
            Dafny.ISequence<Dafny.Rune> _10_b3 = Dafny.Sequence<Dafny.Rune>.Update(Dafny.Sequence<Dafny.Rune>.Update(Dafny.Sequence<Dafny.Rune>.Update((p).dtor_buffer, _6_h0, new Dafny.Rune('?')), _7_h1, new Dafny.Rune('?')), _8_h2, new Dafny.Rune('?'));
            return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let3_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let3_0, _11_dt__update__tmp_h1 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_9_h3, _pat_let4_0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_pat_let4_0, _12_dt__update_hhead_h1 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_10_b3, _pat_let5_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_pat_let5_0, _13_dt__update_hbuffer_h1 => TI59__Print.PrintState.create(_13_dt__update_hbuffer_h1, _12_dt__update_hhead_h1, (_11_dt__update__tmp_h1).dtor_tape))))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements(TI59__Events.Ti59Event.create_EvPrintNotFound(_6_h0, _5_code), TI59__Events.Ti59Event.create_EvPrintFunc(_9_h3, Dafny.Sequence<Dafny.Rune>.UnicodeFromString("???"))));
          }
        }
        {
          BigInteger _14_idx = _source0.dtor_i;
          Dafny.ISequence<Dafny.Rune> _15_s = ((TI59__Print.__default.PrintFuncTable()).Select(_14_idx)).dtor__1;
          BigInteger _16_h0 = (p).dtor_head;
          BigInteger _17_h1 = TI59__Print.__default.PrintHeadStep(_16_h0);
          BigInteger _18_h2 = TI59__Print.__default.PrintHeadStep(_17_h1);
          BigInteger _19_h3 = TI59__Print.__default.PrintHeadStep(_18_h2);
          Dafny.ISequence<Dafny.Rune> _20_b3 = Dafny.Sequence<Dafny.Rune>.Update(Dafny.Sequence<Dafny.Rune>.Update(Dafny.Sequence<Dafny.Rune>.Update((p).dtor_buffer, _16_h0, (_15_s).Select(new BigInteger(2))), _17_h1, (_15_s).Select(BigInteger.One)), _18_h2, (_15_s).Select(BigInteger.Zero));
          return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let6_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let6_0, _21_dt__update__tmp_h2 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_19_h3, _pat_let7_0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_pat_let7_0, _22_dt__update_hhead_h2 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_20_b3, _pat_let8_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_pat_let8_0, _23_dt__update_hbuffer_h2 => TI59__Print.PrintState.create(_23_dt__update_hbuffer_h2, _22_dt__update_hhead_h2, (_21_dt__update__tmp_h2).dtor_tape))))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements(TI59__Events.Ti59Event.create_EvPrintFunc(_19_h3, _15_s)));
        }
      } else if (((bus).dtor_irg) == ((ushort)(2696))) {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let9_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let9_0, _24_dt__update__tmp_h3 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(new BigInteger(19), _pat_let10_0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_pat_let10_0, _25_dt__update_hhead_h3 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim3 = new BigInteger(20);
  var arr3 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim3, "array size exceeds memory limit")];
  for (int i3 = 0; i3 < dim3; i3++) {
    var _26___v1 = (BigInteger) i3;
    arr3[(int)(_26___v1)] = new Dafny.Rune(' ');
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr3);
}))(), _pat_let11_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.Rune>, TI59__Print._IPrintState>(_pat_let11_0, _27_dt__update_hbuffer_h3 => TI59__Print.PrintState.create(_27_dt__update_hbuffer_h3, _25_dt__update_hhead_h3, (_24_dt__update__tmp_h3).dtor_tape))))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements(TI59__Events.Ti59Event.create_EvPrintClear((p).dtor_head, TI59__Print.__default.BufStr20((p).dtor_buffer))));
      } else if (((bus).dtor_irg) == ((ushort)(2712))) {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let12_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let12_0, _28_dt__update__tmp_h4 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(TI59__Print.__default.PrintHeadStep((_pat_let_tv3).dtor_head), _pat_let13_0 => Dafny.Helpers.Let<BigInteger, TI59__Print._IPrintState>(_pat_let13_0, _29_dt__update_hhead_h4 => TI59__Print.PrintState.create((_28_dt__update__tmp_h4).dtor_buffer, _29_dt__update_hhead_h4, (_28_dt__update__tmp_h4).dtor_tape))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements());
      } else if (((bus).dtor_irg) == ((ushort)(2728))) {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let14_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let14_0, _30_dt__update__tmp_h5 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<Dafny.Rune>>, TI59__Print._IPrintState>(Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.Concat((_pat_let_tv4).dtor_tape, Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.FromElements(TI59__Print.__default.BufStr20((_pat_let_tv5).dtor_buffer))), _pat_let15_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<Dafny.Rune>>, TI59__Print._IPrintState>(_pat_let15_0, _31_dt__update_htape_h0 => TI59__Print.PrintState.create((_30_dt__update__tmp_h5).dtor_buffer, (_30_dt__update__tmp_h5).dtor_head, _31_dt__update_htape_h0))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements(TI59__Events.Ti59Event.create_EvPrint((p).dtor_head, TI59__Print.__default.BufStr20((p).dtor_buffer))));
      } else if (((bus).dtor_irg) == ((ushort)(2744))) {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(p, _pat_let16_0 => Dafny.Helpers.Let<TI59__Print._IPrintState, TI59__Print._IPrintState>(_pat_let16_0, _32_dt__update__tmp_h6 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<Dafny.Rune>>, TI59__Print._IPrintState>(Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.Concat((_pat_let_tv6).dtor_tape, Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.FromElements(((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim4 = BigInteger.Zero;
  var arr4 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim4, "array size exceeds memory limit")];
  for (int i4 = 0; i4 < dim4; i4++) {
    var _33___v2 = (BigInteger) i4;
    arr4[(int)(_33___v2)] = new Dafny.Rune(' ');
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr4);
}))())), _pat_let17_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<Dafny.Rune>>, TI59__Print._IPrintState>(_pat_let17_0, _34_dt__update_htape_h1 => TI59__Print.PrintState.create((_32_dt__update__tmp_h6).dtor_buffer, (_32_dt__update__tmp_h6).dtor_head, _34_dt__update_htape_h1))))), Dafny.Sequence<TI59__Events._ITi59Event>.FromElements());
      } else {
        return _System.Tuple2<TI59__Print._IPrintState, Dafny.ISequence<TI59__Events._ITi59Event>>.create(p, Dafny.Sequence<TI59__Events._ITi59Event>.FromElements());
      }
    }
  }

  public interface _IPrintState {
    bool is_PrintState { get; }
    Dafny.ISequence<Dafny.Rune> dtor_buffer { get; }
    BigInteger dtor_head { get; }
    Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> dtor_tape { get; }
    _IPrintState DowncastClone();
  }
  public class PrintState : _IPrintState {
    public readonly Dafny.ISequence<Dafny.Rune> _buffer;
    public readonly BigInteger _head;
    public readonly Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> _tape;
    public PrintState(Dafny.ISequence<Dafny.Rune> buffer, BigInteger head, Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> tape) {
      this._buffer = buffer;
      this._head = head;
      this._tape = tape;
    }
    public _IPrintState DowncastClone() {
      if (this is _IPrintState dt) { return dt; }
      return new PrintState(_buffer, _head, _tape);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Print.PrintState;
      return oth != null && object.Equals(this._buffer, oth._buffer) && this._head == oth._head && object.Equals(this._tape, oth._tape);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._buffer));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._head));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._tape));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Print.PrintState.PrintState";
      s += "(";
      s += this._buffer.ToVerbatimString(true);
      s += ", ";
      s += Dafny.Helpers.ToString(this._head);
      s += ", ";
      s += Dafny.Helpers.ToString(this._tape);
      s += ")";
      return s;
    }
    private static readonly TI59__Print._IPrintState theDefault = create(Dafny.Sequence<Dafny.Rune>.Empty, BigInteger.Zero, Dafny.Sequence<Dafny.ISequence<Dafny.Rune>>.Empty);
    public static TI59__Print._IPrintState Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Print._IPrintState> _TYPE = new Dafny.TypeDescriptor<TI59__Print._IPrintState>(TI59__Print.PrintState.Default());
    public static Dafny.TypeDescriptor<TI59__Print._IPrintState> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IPrintState create(Dafny.ISequence<Dafny.Rune> buffer, BigInteger head, Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> tape) {
      return new PrintState(buffer, head, tape);
    }
    public static _IPrintState create_PrintState(Dafny.ISequence<Dafny.Rune> buffer, BigInteger head, Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> tape) {
      return create(buffer, head, tape);
    }
    public bool is_PrintState { get { return true; } }
    public Dafny.ISequence<Dafny.Rune> dtor_buffer {
      get {
        return this._buffer;
      }
    }
    public BigInteger dtor_head {
      get {
        return this._head;
      }
    }
    public Dafny.ISequence<Dafny.ISequence<Dafny.Rune>> dtor_tape {
      get {
        return this._tape;
      }
    }
  }

  public interface _IFuncIdx {
    bool is_FNone { get; }
    bool is_FFound { get; }
    BigInteger dtor_i { get; }
    _IFuncIdx DowncastClone();
  }
  public abstract class FuncIdx : _IFuncIdx {
    public FuncIdx() {
    }
    private static readonly TI59__Print._IFuncIdx theDefault = create_FNone();
    public static TI59__Print._IFuncIdx Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Print._IFuncIdx> _TYPE = new Dafny.TypeDescriptor<TI59__Print._IFuncIdx>(TI59__Print.FuncIdx.Default());
    public static Dafny.TypeDescriptor<TI59__Print._IFuncIdx> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IFuncIdx create_FNone() {
      return new FuncIdx_FNone();
    }
    public static _IFuncIdx create_FFound(BigInteger i) {
      return new FuncIdx_FFound(i);
    }
    public bool is_FNone { get { return this is FuncIdx_FNone; } }
    public bool is_FFound { get { return this is FuncIdx_FFound; } }
    public BigInteger dtor_i {
      get {
        var d = this;
        return ((FuncIdx_FFound)d)._i;
      }
    }
    public abstract _IFuncIdx DowncastClone();
  }
  public class FuncIdx_FNone : FuncIdx {
    public FuncIdx_FNone() : base() {
    }
    public override _IFuncIdx DowncastClone() {
      if (this is _IFuncIdx dt) { return dt; }
      return new FuncIdx_FNone();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Print.FuncIdx_FNone;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Print.FuncIdx.FNone";
      return s;
    }
  }
  public class FuncIdx_FFound : FuncIdx {
    public readonly BigInteger _i;
    public FuncIdx_FFound(BigInteger i) : base() {
      this._i = i;
    }
    public override _IFuncIdx DowncastClone() {
      if (this is _IFuncIdx dt) { return dt; }
      return new FuncIdx_FFound(_i);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Print.FuncIdx_FFound;
      return oth != null && this._i == oth._i;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._i));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Print.FuncIdx.FFound";
      s += "(";
      s += Dafny.Helpers.ToString(this._i);
      s += ")";
      return s;
    }
  }
} // end of namespace TI59__Print
namespace _module {

} // end of namespace _module
