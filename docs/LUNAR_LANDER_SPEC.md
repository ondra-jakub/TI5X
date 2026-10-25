# Lunar Lander (Přistání na Měsíci) — Numerical Model & Technical Specification
**Target Platforms:** Texas Instruments TI-58, TI-58C, TI-59 & Compatible Emulators (`TI5X.Avalonia`)

---

## 1. Physical Simulation Model & Numerical Contracts

### 1.1 Physical Constants & Units
The simulation implements a 1-dimensional vertical descent model in the lunar gravitational field using standard discrete time-steps.

| Constant / Parameter | Symbol | Value | Unit | Description |
|:---|:---:|:---:|:---:|:---|
| Time Step | $\Delta t$ | `1.0` | $\text{s}$ | Discrete simulation period per turn / keystroke cycle. |
| Lunar Gravity | $g$ | `5.0` | $\text{ft/s}^2$ | Constant downward acceleration (real lunar $g \approx 5.3\text{ ft/s}^2$; simplified here to exactly $5$ for easy mental arithmetic). |
| Safe Landing Limit | $v_{safe}$ | `-5.0` | $\text{ft/s}$ | Executable rule: touchdown is safe iff $v_{end} \ge -5.0\text{ ft/s}$, which includes every positive end velocity; $v_{end} < -5.0$ is a crash. |
| Initial Altitude | $x_0$ | `2500` | $\text{ft}$ | Starting altitude above lunar ground level ($x = 0$). |
| Initial Velocity | $v_0$ | `-500` | $\text{ft/s}$ | Starting vertical velocity (negative indicates descent). |
| Initial Propellant | $f_0$ | `600` | $\text{lbs}$ (units) | Starting fuel supply in propellant tanks. |

### 1.2 Sign & Direction Conventions
- **Altitude ($x$):** $x \ge 0$. Distance above lunar surface in feet. Ground contact occurs when $x \le 0$.
- **Velocity ($v$):** Velocity in ft/s. Negative ($v < 0$) indicates downward descent toward the surface; positive ($v > 0$) indicates upward ascent away from the surface.
- **Burn / Thrust ($B$):** Fuel mass burned per second; upward thrust acceleration is $B\text{ ft/s}^2$ (1 lb of propellant per second gives 1 ft/s² by normalization). **Input contract: $B$ is a non-negative integer.** The program does not reject violations: a negative $B$ passes the clamp ($B \le R00$), so $R00 - B$ *increases* fuel (deterministic fuel creation) and gives $a < -5$; a fractional $B$ makes $v$ fractional, which leaks into the altitude digits of the HUD (see 4.2).
- **Net Acceleration ($a$):** $a = B - g = B - 5\text{ ft/s}^2$. Upward acceleration is positive ($B > 5$), neutral hover is zero ($B = 5$), and downward free-fall acceleration is negative ($B < 5$).

---

## 2. Discrete State Transition Equations

For each turn $k \to k+1$ with requested burn $B_{req}$:

### 2.1 Fuel Clamping & Consumption
To prevent the pilot from burning non-existent fuel:
$$B_{actual} = \min(B_{req}, R00)$$
$$R00_{k+1} = R00_k - B_{actual}$$

- If $B_{req} \le R00_k$: The requested burn is executed completely. The clamp guards only the upper bound; negative $B_{req}$ is unsupported (it passes the clamp, adds $|B_{req}|$ to fuel and accelerates the lander downward by more than gravity). Restrict $B_{req} \ge 0$.
- If $B_{req} > R00_k$: The engine burns only the remaining propellant $R00_k$, and fuel drops to $0$.
- When $R00 = 0$: Any subsequent burn input results in $B_{actual} = 0$, causing free-fall under gravity ($a = -5\text{ ft/s}^2$).

### 2.2 Kinematic Updates
The equations of motion over the discrete $1\text{ s}$ interval with constant acceleration $a = B_{actual} - 5$ are:
$$a = B_{actual} - 5$$
$$x_{k+1} = x_k + v_k \Delta t + \frac{1}{2} a (\Delta t)^2 = x_k + v_k + \frac{a}{2}$$
$$v_{k+1} = v_k + a \Delta t = v_k + a$$

*Note:* $x_{k+1}$ uses the exact trapezoidal kinematic formula for constant acceleration over $1\text{ s}$, avoiding first-order Euler truncation errors.

---

## 3. Touchdown & Ground Contact Evaluation

Ground contact is detected only when the **end-of-step** altitude satisfies $x_{k+1} \le 0$.

### 3.1 Discrete 1-Second Step Contract (Calculator Implementation)
This program evaluates touchdown at the completion of the 1-second discrete turn (a design choice of this spec; no claim is made about how other historical programs handle it):
1. Altitude is clamped to ground level: $R02 = 0$.
2. Landing speed is the end-of-step velocity: $v_{touchdown} = v_{k+1} = v_k + a$.
3. Touchdown criterion:
   - **Safe Landing (Victory):** if $v_{touchdown} \ge -5.0\text{ ft/s}$ (single `x≥t` test with $T=-5$). This is **not** equivalent to $|v| \le 5$: it also accepts every positive end velocity. Branch to `Lbl D'`, display the touchdown velocity in `Fix 2`, halt on `R/S`.
   - **Crash (Fatal Impact):** if $v_{touchdown} < -5.0\text{ ft/s}$. Branch to `Lbl E'`, display the impact velocity, trigger an error state (`0 ÷ 0`).
4. **Known limitations of the endpoint-only rule (Step 2 implements it as is):**
   - *Spurious safe on rebound:* if the step ends at $x \le 0$ with $v_{end} > 0$ the landing is reported safe even though the true impact may be fast. Example: $x=50, v=-200, B=255$ ($a=250$) gives $x_{k+1}=-25$, $v_{k+1}=+50$ → "safe", while the continuous impact speed is $\approx -122\text{ ft/s}$.
   - *Missed crossing:* if the lander dips below $0$ inside the step but is above $0$ again at its end ($a>0$), no contact is ever detected. Example: $x=1, v=-20, B=45$ gives $x_{k+1}=+1$, $v_{k+1}=+20$, although $x(0.5)=-4$.
   - If the step ends at $x \le 0$, contact is detected in that same turn.

### 3.2 Comparison: Discrete Step vs. Continuous Interpolated Impact
In continuous Newtonian physics, ground impact occurs at an intermediate time $\tau \in (0, 1]\text{ s}$ where $x_k + v_k \tau + \frac{1}{2} a \tau^2 = 0$:
$$\tau = \begin{cases} \frac{-x_k}{v_k} & \text{if } a = 0 \\ \frac{-v_k - \sqrt{v_k^2 - 2 a x_k}}{a} & \text{if } a \ne 0 \end{cases}$$
$$v_{impact} = v_k + a \tau = -\sqrt{v_k^2 - 2 a x_k}$$

**Why the Discrete Step Model is preferred for TI-58/TI-59:**
1. **Simplicity:** Solving the quadratic for $\tau$ would add roughly $35\text{–}45$ program steps, a square root and extra branches. It would still fit the default partitions, but the endpoint test is simpler to key in, verify and explain, so it is chosen deliberately.
2. **Accepted inaccuracy:** the limitations listed in 3.1 (rebound classified by end velocity, missed dip-and-return) are accepted consequences of this choice.

---

## 4. Combined HUD Display ($-V.AAAA$) Viability & Edge Cases

To display both velocity and altitude simultaneously on the single 10-digit LED display of the TI-58/59, the program encodes both values into a combined HUD number using 4 decimal places (`2nd Fix 4`):
$$\text{HUD} = v - \frac{x}{10000}$$
This is the single canonical formula (no alternative is used). It is a display value only; registers hold exact $v$ and $x$.

### 4.1 Nominal Descent Mode ($v < 0$, $0 \le x < 10000$)
- **Example:** $v = -500\text{ ft/s}$, $x = 2500\text{ ft}$.
- Calculation: $-500 - \frac{2500}{10000} = -500.2500$.
- With `2nd Fix 4`, display shows: `-500.2500`.
- **Reading:** Integer part is vertical speed ($-500\text{ ft/s}$), decimal part is altitude ($2500\text{ ft}$).
- **Caveat:** With integer burns $B \ge 0$, $v$ is always an integer and $x$ a multiple of $0.5$ (exact in the calculator's decimal arithmetic).

### 4.2 Fractional Altitude Handling
- When acceleration $a$ is odd (e.g. $B=0 \implies a=-5$), the term $a/2 = -2.5\text{ ft}$ introduces a half-foot fraction ($.5$).
- **Example:** Turn 1 with $B=100$: $a = 95$, $a/2 = 47.5$, $x = 2500 - 500 + 47.5 = 2047.5\text{ ft}$, $v = -405\text{ ft/s}$.
- Calculation: $-405 - 0.20475 = -405.20475$.
- Display under `Fix 4`: rounds to `-405.2048`.
- Precision: $x/10000$ with $x$ a multiple of $0.5$ needs 5 decimals; `Fix 4` rounds the fifth digit half-up ($2047.5 \to$ shown as `.2048`, i.e. altitude displayed rounded up to a whole foot). The TI-58/59 compute with 13 digits internally and display 10, so the HUD sum is not truncated before rounding. The exact altitude ($2047.5$) stays in $R02$ (query with `D`).
- **Fractional $v$ corrupts the HUD:** a fractional burn leaves a fractional part in $v$ that adds to the altitude decimals. Example: $v=-405.5$, $x=2047.5$ shows `-405.7048` instead of `-405.2048`. Hence integer $B$ is required.

### 4.3 Upward Flight / Ascent ($v > 0$)
- For $v>0$ and $x>0$ the formula always borrows from the integer part: $v=+50$, $x=2500$ displays `49.7500` (reads as velocity 49, altitude 7500 — wrong on both). For $v=0$, $x=2500$ it shows `-0.2500`.
- **Operational Rule:** the HUD is meaningful only for $v < 0$. During ascent the pilot must use telemetry keys `D` (altitude) and `E` (velocity).

### 4.4 Altitude Range & Display Width Limits
- **Altitude Range:** Initial altitude is $2500\text{ ft}$, but excessive thrust can drive altitude up to $\approx 3200\text{ ft}$ during ascent. The 4 decimal places support altitudes up to $9999.9\text{ ft}$.
- **Hardware Display Width:** TI-58/59 features a 10-digit 7-segment LED display with negative sign. With `Fix 4`, numbers formatted as `-DDDDD.DDDD` (up to 5 integer digits and 4 decimal digits) fit cleanly without exponential notation.

---

## 5. Memory Layout & Register Allocation

### 5.1 Register Map

| Register | Variable | Initial Value | Description |
|:---:|:---:|:---:|:---|
| `R00` | Propellant ($f$) | `600.0` | Remaining fuel supply (lbs). Decremented by burn each turn. |
| `R01` | Velocity ($v$) | `-500.0` | Vertical speed (ft/s). Negative = descent, Positive = ascent. |
| `R02` | Altitude ($x$) | `2500.0` | Height above lunar surface (ft). Ground is $0.0$. |
| `R03` | Burn ($B$) | — | Propellant burned in current 1-second turn (clamped to $R00$). |
| `R04` | Acceleration ($a$) | — | Net vertical acceleration $a = B - 5\text{ ft/s}^2$. |
| `T` | Test Register | — | Calculator test scratchpad used for comparisons (`x↔t`, `x≥t`, `x=t`). |

### 5.2 Calculator Partitioning (`Op 17`) Compatibility

- **TI-58 Default Partition:** 240 program steps / 30 data registers (`R00`–`R29`).
- **TI-59 Default Partition:** 480 program steps / 60 data registers (`R00`–`R59`).
- **Program Requirement:** 5 data registers (`R00`–`R04`) and about 140 program bytes (estimate counting every operand byte of the reference listing; the final count is fixed in Step 2).
- **Conclusion:** even with growth margin this is far below 240 (TI-58) and 480 (TI-59) steps, so no repartitioning (`Op 17`) is needed on either machine.

---

## 6. TI-58 / TI-59 Keycode Conventions & Instruction Encoding

### 6.1 Key Matrix & 2-Digit Opcodes
On TI-58/59 calculators, instruction opcodes correspond to `Row * 10 + Col` (with Row 1..9, Col 1..5):

```text
Row 1: [A] 11    [B] 12    [C] 13    [D] 14    [E] 15
2nd:   [A'] 16   [B'] 17   [C'] 18   [D'] 19   [E'] 10  <-- Note E' is opcode 10
Row 2: [2nd] 21  [INV] 22  [ln x] 23 [CE] 24   [CLR] 25
Row 3: [LRN] 31  [x↔t] 32  [x²] 33   [√x] 34   [1/x] 35
Row 4: [SST] 41  [STO] 42  [RCL] 43  [SUM] 44  [y^x] 45
Row 5: [BST] 51  [EE] 52   [(] 53    [)] 54    [÷] 55
Row 6: [GTO] 61  [7] 07    [8] 08    [9] 09    [×] 65   (2nd: Fix=58, x=t=67, Op=69)
Row 7: [SBR] 71  [4] 04    [5] 05    [6] 06    [-] 75   (2nd: Lbl=76, x≥t=77)
Row 8: [RST] 81  [1] 01    [2] 02    [3] 03    [+] 85
Row 9: [R/S] 91  [0] 00    [.] 93    [+/-] 94  [=] 95
```

### 6.2 Multi-Byte Sequence Rules in Program Memory
1. **Numeric Digits:** In program memory, digit keys `0` through `9` are encoded as single-byte values `00` through `09`.
2. **Memory Instructions:** Register operations occupy 2 program steps:
   - `STO 00` $\to$ `42` (`STO`), `00` (Register 00).
   - `RCL 02` $\to$ `43` (`RCL`), `02` (Register 02).
   - `SUM 01` $\to$ `44` (`SUM`), `01` (Register 01).
   - `INV SUM 00` $\to$ `22` (`INV`), `44` (`SUM`), `00` (Register 00) — occupies 3 steps.
3. **Display Formatting:** `2nd Fix 4` occupies 2 steps: `58` (`FIX`), `04` (4 decimals).
4. **Label Definitions:** `2nd Lbl A` $\to$ `76` (`LBL`), `11` (`A`).
5. **Conditional Branching:**
   - `2nd x≥t 2nd A'` $\to$ `77` (`x≥t`), `16` (`A'`).
   - `INV 2nd x≥t 2nd B'` $\to$ `22` (`INV`), `77` (`x≥t`), `17` (`B'`).
   - `2nd x=t 2nd B'` $\to$ `67` (`x=t`), `17` (`B'`).

---

## 7. Telemetry & User Key Contract

| Key | Label | Function | Display Output |
|:---:|:---:|:---|:---|
| `A` | `Lbl A` | Initialize simulation state ($f=600, v=-500, x=2500$) | Initial HUD `-500.2500` (halted on `R/S`) |
| `B` / `R/S` | `Lbl B` | Execute turn with display register $X$ as burn input $B$ (`R/S` only while halted at the HUD; after a `C`/`D`/`E` query press `B`) | Updated HUD $-V.AAAA$ (halted on `R/S`) |
| `C` | `Lbl C` | Query remaining propellant ($R00$) | Fuel mass (e.g. `600.0000`), halts on `R/S` |
| `D` | `Lbl D` | Query current exact altitude ($R02$) | Altitude in ft (e.g. `2047.5000`), halts on `R/S` |
| `E` | `Lbl E` | Query current velocity ($R01$) | Velocity in ft/s (e.g. `-405.0000`), halts on `R/S` |
| `D'` | `Lbl D'` | Safe landing handler ($v \ge -5.0$, includes $v>0$) | Soft touchdown speed in `Fix 2` (integer for integer burns, e.g. `-4.00`, `0.00`) |
| `E'` | `Lbl E'` | Crash handler ($v < -5.0$) | Flashing error state (`0 ÷ 0`) |

---

## 8. Emulator Considerations (`TI5X.Avalonia`)

When executing this program on desktop/mobile emulators such as `TI5X.Avalonia`:
1. **Conditional Branch Opcodes:** The execution engine dispatches `KEY_X_GE_T` (`77`) and `KEY_X_EQ_T` (`67`) conditionals against the $T$ register and skips branch targets when false.
2. **Sequential Label Declarations:** Sequential execution skips over `Lbl` declaration operands (`76 nn`) as non-executable markers rather than invoking subroutines.
3. **Partition Bounds:** Memory registers $R00$–$R04$ reside comfortably within default partition bank 0.

---

## 9. Complete Keystroke Program Listing (142 Steps / Bytes)

Every program step occupies exactly one byte in program memory (steps `000` through `141`).

```text
Step | Key Entry       | Code | Comment / Operation
-----+-----------------+------+------------------------------------------------------
000  | 2nd Lbl A       | 76   | [Lbl A] Initialize Game
001  |                 | 11   | Label identifier A
002  | 6               | 06   | Enter 600
003  | 0               | 00   |
004  | 0               | 00   |
005  | STO             | 42   | R00 = 600 (Initial Fuel)
006  | 00              | 00   | (register 00)
007  | 5               | 05   | Enter -500
008  | 0               | 00   |
009  | 0               | 00   |
010  | +/-             | 94   | Negate
011  | STO             | 42   | R01 = -500 (Initial Velocity)
012  | 01              | 01   | (register 01)
013  | 2               | 02   | Enter 2500
014  | 5               | 05   |
015  | 0               | 00   |
016  | 0               | 00   |
017  | STO             | 42   | R02 = 2500 (Initial Altitude)
018  | 02              | 02   | (register 02)
019  | GTO             | 61   | Jump to HUD display
020  | 2nd C'          | 18   | (Lbl C')
-----+-----------------+------+------------------------------------------------------
021  | 2nd Lbl B       | 76   | [Lbl B] Turn entry (Burn B in display register X)
022  |                 | 12   | Label identifier B
023  | STO             | 42   | R03 = requested burn B
024  | 03              | 03   | (register 03)
025  | x↔t             | 32   | T = requested burn B
026  | RCL             | 43   | X = available fuel R00
027  | 00              | 00   | (register 00)
028  | 2nd x≥t         | 77   | If Fuel >= Burn (Fuel OK)
029  | 2nd A'          | 16   | goto Lbl A'
030  | RCL             | 43   | Otherwise Burn = remaining fuel
031  | 00              | 00   | (register 00)
032  | STO             | 42   | R03 = R00
033  | 03              | 03   | (register 03)
-----+-----------------+------+------------------------------------------------------
034  | 2nd Lbl 2nd A'  | 76   | [Lbl A'] Physics update
035  |                 | 16   | Label identifier A'
036  | RCL             | 43   | Recall Burn (R03)
037  | 03              | 03   | (register 03)
038  | INV             | 22   | R00 = R00 - Burn (deduct propellant)
039  | SUM             | 44   |
040  | 00              | 00   | (register 00)
041  | RCL             | 43   | Recall Burn
042  | 03              | 03   | (register 03)
043  | -               | 75   | Subtract gravity (g = 5)
044  | 5               | 05   |
045  | =               | 95   | a = Burn - 5
046  | STO             | 42   | R04 = net acceleration a
047  | 04              | 04   | (register 04)
048  | (               | 53   | Calculate altitude change: x_new = x + v + a/2
049  | RCL             | 43   | Recall velocity v (R01)
050  | 01              | 01   | (register 01)
051  | +               | 85   |
052  | (               | 53   |
053  | RCL             | 43   | Recall acceleration a (R04)
054  | 04              | 04   | (register 04)
055  | ÷               | 55   | Divide by 2
056  | 2               | 02   |
057  | )               | 54   |
058  | )               | 54   |
059  | SUM             | 44   | R02 = R02 + (v + a/2)
060  | 02              | 02   | (register 02)
061  | RCL             | 43   | Recall acceleration a (R04)
062  | 04              | 04   | (register 04)
063  | SUM             | 44   | R01 = R01 + a (update velocity v_new = v + a)
064  | 01              | 01   | (register 01)
065  | 0               | 00   | Set T = 0 for ground comparison
066  | x↔t             | 32   | (T = 0)
067  | RCL             | 43   | X = Altitude (R02)
068  | 02              | 02   | (register 02)
069  | INV             | 22   | If Altitude < 0 (touchdown / subsurface contact)
070  | 2nd x≥t         | 77   |
071  | 2nd B'          | 17   | goto Lbl B'
072  | 2nd x=t         | 67   | If Altitude == 0 (exact surface touchdown)
073  | 2nd B'          | 17   | goto Lbl B'
-----+-----------------+------+------------------------------------------------------
074  | 2nd Lbl 2nd C'  | 76   | [Lbl C'] HUD Formatter & Wait
075  |                 | 18   | Label identifier C'
076  | (               | 53   | Display = v - (x / 10000)
077  | RCL             | 43   | Recall velocity v (R01)
078  | 01              | 01   | (register 01)
079  | -               | 75   |
080  | (               | 53   |
081  | RCL             | 43   | Recall altitude x (R02)
082  | 02              | 02   | (register 02)
083  | ÷               | 55   |
084  | 1               | 01   | Divide by 10000
085  | 0               | 00   |
086  | 0               | 00   |
087  | 0               | 00   |
088  | 0               | 00   |
089  | )               | 54   |
090  | )               | 54   |
091  | =               | 95   | Compute HUD value
092  | 2nd Fix         | 58   | Set display format to 4 decimals (-V.AAAA)
093  | 4               | 04   |
094  | R/S             | 91   | Stop execution & wait for pilot's next burn entry
095  | GTO             | 61   | Loop to next turn
096  | B               | 12   | (Lbl B)
-----+-----------------+------+------------------------------------------------------
097  | 2nd Lbl 2nd B'  | 76   | [Lbl B'] Ground Touchdown Evaluation
098  |                 | 17   | Label identifier B'
099  | 0               | 00   | Reset altitude R02 = 0
100  | STO             | 42   |
101  | 02              | 02   | (register 02)
102  | 5               | 05   | Set T = -5.0 (Safe landing velocity threshold)
103  | +/-             | 94   |
104  | x↔t             | 32   | (T = -5.0)
105  | RCL             | 43   | X = Touchdown velocity (R01)
106  | 01              | 01   | (register 01)
107  | 2nd x≥t         | 77   | If v >= -5.0 (Safe landing)
108  | 2nd D'          | 19   | goto Lbl D'
-----+-----------------+------+------------------------------------------------------
109  | 2nd Lbl 2nd E'  | 76   | [Lbl E'] CRASH State (Fatal Impact)
110  |                 | 10   | Label identifier E' (Opcode 10)
111  | RCL             | 43   | Recall fatal impact velocity (R01)
112  | 01              | 01   | (register 01)
113  | 2nd Fix         | 58   | Format to 2 decimals
114  | 2               | 02   |
115  | 0               | 00   | Force blinking Error display via division by zero (0 ÷ 0)
116  | ÷               | 55   |
117  | 0               | 00   |
118  | =               | 95   | Triggers flashing error state
119  | R/S             | 91   | Halt in crash state
-----+-----------------+------+------------------------------------------------------
120  | 2nd Lbl 2nd D'  | 76   | [Lbl D'] SAFE LANDING (Victory!)
121  |                 | 19   | Label identifier D'
122  | RCL             | 43   | Recall touchdown velocity (R01)
123  | 01              | 01   | (register 01)
124  | 2nd Fix         | 58   | Format to 2 decimals
125  | 2               | 02   |
126  | R/S             | 91   | Halt: Victory!
-----+-----------------+------+------------------------------------------------------
127  | 2nd Lbl C       | 76   | [Lbl C] Telemetry Query: Remaining Fuel
128  |                 | 13   | Label identifier C
129  | RCL             | 43   | Show R00
130  | 00              | 00   | (register 00)
131  | R/S             | 91   | Halt display
-----+-----------------+------+------------------------------------------------------
132  | 2nd Lbl D       | 76   | [Lbl D] Telemetry Query: Current Altitude
133  |                 | 14   | Label identifier D
134  | RCL             | 43   | Show R02
135  | 02              | 02   | (register 02)
136  | R/S             | 91   | Halt display
-----+-----------------+------+------------------------------------------------------
137  | 2nd Lbl E       | 76   | [Lbl E] Telemetry Query: Current Velocity
138  |                 | 15   | Label identifier E
139  | RCL             | 43   | Show R01
140  | 01              | 01   | (register 01)
141  | R/S             | 91   | Halt display
```

---

## 10. Magnetic Card Definition (`card00`)

```xml
<carddef style="system">
  <label slot="titleleft">LUNAR LANDER</label>
  <label slot="titleright">TI-58 / TI-59</label>
  <label slot="a">INIT</label>
  <label slot="b">BURN</label>
  <label slot="c">FUEL</label>
  <label slot="d">ALT</label>
  <label slot="e">VEL</label>
  <label slot="aprime">PHYS</label>
  <label slot="bprime">TOUCH</label>
  <label slot="cprime">HUD</label>
  <label slot="dprime">WIN</label>
  <label slot="eprime">CRASH</label>
</carddef>
```

---

## 11. User Guide — How to Play

### 11.1 Loading the program
- **TI-58 / TI-59 (keyboard entry):** `2nd CP`, `LRN`, key in steps `000`–`141` from §9 (each code = one keystroke sequence, e.g. `STO 0 0` is entered as `STO 0 0` and stored as `42 00`), `LRN`, `RST`. Check with `SST`/`BST` against the address column.
- **TI-59 (magnetic card):** after keying in, store bank 1 with `1 2nd Write` and insert side 1 of a card. Later reload with `CLR` + insert card. Write the labels from §10 on the card strip.
- **TI5X.Avalonia:** File → Open Program → `src/TI5X.Avalonia/Assets/lunar_lander.ti5p`. The card strip shows `INIT / BURN / FUEL / ALT / VEL`.

### 11.2 Playing
1. Press `A`. Display: `-500.2500` (descending at 500 ft/s, altitude 2500 ft).
2. Enter the burn for the next second (non-negative integer) and press `R/S` (or `B`).
3. Read the HUD `-VVV.AAAA`: integer part = sink rate, four decimals = altitude in feet (rounded half-up, e.g. `2029.5` ft shows as `.2030`).
4. Any time while halted: `C` = fuel, `D` = altitude, `E` = velocity. **After such a query, enter the burn and press `B`** — pressing `R/S` would continue into the next query routine instead of a flight turn.
5. Repeat until touchdown.

### 11.3 End of game
- **Safe landing** ($v \ge -5$ ft/s at the end of the step in which $x \le 0$): display shows the touchdown speed in `Fix 2`, e.g. `0.00` or `-4.00`. No flashing.
- **Crash** ($v < -5$): the display flashes (error caused by `0 ÷ 0`). On real hardware the flashing value is the error value, not the impact speed; press `CLR`, then `E` to see the impact velocity.
- **Fuel exhausted:** burns larger than the remaining fuel are clamped; with `0` fuel the module falls freely until touchdown is evaluated.
- New game: press `A` (on hardware press `CLR` first if the display is flashing).

### 11.4 Worked example — a complete winning game (verified in TI5X test suite)

| Turn | Burn entered | Fuel $R00$ | Altitude $R02$ | Velocity $R01$ | Display |
|:---:|:---:|:---:|:---:|:---:|:---:|
| start | `A` | 600 | 2500 | −500 | `-500.2500` |
| 1 | 64 | 536 | 2029.5 | −441 | `-441.2030` |
| 2 | 0 | 536 | 1586 | −446 | `-446.1586` |
| 3 | 0 | 536 | 1137.5 | −451 | `-451.1138` |
| 4 | 0 | 536 | 684 | −456 | `-456.0684` |
| 5 | 0 | 536 | 225.5 | −461 | `-461.0226` |
| 6 | 466 | 70 | 0 (end of step −5 → clamped) | 0 | `0.00` — **landed** |

Strategy hint: fuel is the total velocity change you can buy (1 unit = 1 ft/s), and every second costs 5 ft/s to gravity. Short descents with one late, large braking burn are the most fuel-efficient.

---

## 12. Compatibility & Emulator Notes

- **TI-58 / TI-58C:** 142 steps and registers R00–R04 fit the default partition (240 steps / 30 registers); no `Op 17` needed. No card reader — key the program in (TI-58C keeps it in Constant Memory).
- **TI-59:** default 480 steps / 60 registers; one card side (bank 1, steps 000–239) holds the whole program.
- **TI5X:** verified by the automated suite (`dotnet test --solution TI5X.slnx`: init, free fall, hover, walkthrough, clamping, safe/rebound/crash boundaries, telemetry, resume-after-query, full winning game, `.ti5p` round-trip and asset consistency). Arithmetic uses binary `double`, not 13-digit BCD; with integer burns all values are exact halves, so the displayed results are identical.
- **Other emulators (Hrastprogrammer TI-59E, ti58c.phweb.me, go59c, TI59R):** the program uses only standard opcodes (`STO/RCL/SUM/INV SUM`, `x↔t`, `x≥t`, `x=t`, `Lbl`, `GTO`, `Fix`, `R/S`) and should run unchanged, but it has **not** been tested on them or on physical hardware.

---

## 13. Heritage & Sources

This program is an adaptation of the classic 1970s "Lunar Lander" calculator game: SR-56 *Applications Library* (600 fuel, −500 ft/s, 2500 ft, combined `−V.AAAA` display), HP-25 *Moon Landing Simulator* (500 ft, −50 ft/s, 120 fuel), HP-55 (PPC Journal V4N4) and HP-15C *Moonlanding*; a TI-58/59 version was also published in the Czechoslovak magazine VTM. Unlike the SR-56 original (absolute step addresses, impact velocity by $\sqrt{v^2+2gx}$), this version uses symbolic labels and evaluates touchdown at the end of each 1 s step (§3).

Sources: rskey.org/gene/calcgene/56lander.htm; hpmuseum.org (HP-25/HP-55 Moon Landing); hp-15c-simulator.de; ti58c.phweb.me and hsl.wz.cz (TI-58/59 instruction set and key codes); oldcomp.cz (VTM "Přistání na Měsíci"); en.wikipedia.org/wiki/TI-59_/_TI-58; calcu59.ch.
