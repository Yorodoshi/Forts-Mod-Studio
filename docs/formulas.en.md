# FMS variables and formulas

[Русское руководство](formulas.md)

## Getting started

1. Open Scenarios and create a variable in the panel on the right.
2. Set its name, type and initial expression. For example: `power`, Floating-point, `sqrt(81) * 2`.
3. Use it in a block's numeric field: `$power + 5`.
4. To change it during a match, add an Edit variable action, select the variable and an operation.
5. Open Formula guide for local help. The window supports search, Ctrl+F and F3 / Shift+F3.

The panel on the right lists declared variables and the computed **initial** values of the current project. It does not connect to a running match or show its live state. Initial expressions are evaluated when the project is prepared; changing a variable during the match does not automatically recompute the other variables. An action evaluates its expression again when it runs in Forts.

## Names and types

Use 1–40 Latin letters, digits and `_`; the first character must be a Latin letter or `_`. References can be `$power` or `power`; `$power` is recommended for clarity. Names are case-sensitive: `power` and `Power` differ. `speed`, `hp`, `damage`, `repeat_index`, `pi`, `e` and `tau` are reserved. Avoid using function names for your own variables too.

| Editor type | Value and conversion |
| --- | --- |
| Integer | Signed 32-bit number: −2147483648 to 2147483647. Expression results are truncated toward zero: `3.9` → `3`, `-3.9` → `-3`. Out-of-range results are errors. |
| Floating-point | Double-precision number. Only finite values are accepted; infinity and NaN are errors. |
| List | Up to 4096 finite numbers. Examples: `[5, 10.5, -2]` or `range(1, 12)`. Nested lists, strings and objects are unsupported. |

A number and a list do not convert into one another automatically. Use `at`, `sum`, `mean` or `len` to obtain a number from a list. Indices start at **1**, must be integers and must refer to an existing element.

Initial expressions may reference other declared variables in any order: `base = 10`, `power = $base * 3`. A cycle such as `a = $b`, `b = $a`, a self-reference or a missing variable is an error. The panel and project validation show these errors.

Global for the match scope stores one value for the entire match. Per object scope stores an independent value for each scenario object. List assignments copy values: editing one list does not change copies previously assigned to other variables or objects.

Renaming updates references in the project's expressions and blocks. Deleting an in-use variable is blocked: change or remove its references first. After changing a type, check that the expression and block operations remain compatible.

## Expression syntax

Use a decimal point: `1.25`. Scientific notation is supported: `2.5e3`, `1e-4`. Separate function arguments and list entries with commas. Trigonometric functions use radians.

| Syntax | Meaning |
| --- | --- |
| `+`, `-`, `*`, `/` | Addition, subtraction, multiplication, division |
| `%` | Remainder using a quotient rounded down: `-5 % 3` → `1` |
| `^` | Exponentiation: `2 ^ 3` |
| `-x`, `+x` | Unary sign |
| `( ... )` | Explicit evaluation order: `(2 + 3) * 4` |
| `pi`, `e`, `tau` | π, Euler's number, 2π |
| `[1, 2, 3]` | Numeric list |

Precedence: parentheses and calls, powers, unary signs, multiplication/division/remainder, addition/subtraction. Powers associate to the right: `2^3^2` → `512`. `-2^2` → `-4`, `2^-2` → `0.25`. Use parentheses for complex powers and negative bases: `(-2)^2`, `-(2^2)`, `(2^3)^2`.

Expressions use a restricted mathematical language. They do not execute Lua code, commands, assignments within an expression, file access or network access. Change values through a separate action.

## Function reference

### Numbers and intervals

| Function | Result / example |
| --- | --- |
| `abs(x)` | Absolute value: `abs(-5)` → `5` |
| `sqrt(x)`, `cbrt(x)` | Square and cube roots: `sqrt(81)` → `9`, `cbrt(-8)` → `-2` |
| `pow(x, y)` | Power; same purpose as `x ^ y` |
| `exp(x)`, `ln(x)` | e raised to x; natural logarithm |
| `log(x)`, `log(x, base)` | Natural logarithm; logarithm in a given base: `log(8, 2)` → `3` |
| `log10(x)` | Base-10 logarithm |
| `floor(x)`, `ceil(x)` | Round down/up: `floor(-1.2)` → `-2` |
| `round(x)` | Nearest integer, halves away from zero: `round(-2.5)` → `-3` |
| `sign(x)` | −1, 0 or 1 |
| `min(a, b, ...)`, `max(a, b, ...)` | Minimum/maximum of two or more numbers, or a single list: `max($values)` |
| `clamp(x, lo, hi)` | Restrict to an interval: `clamp(15, 0, 10)` → `10` |
| `lerp(a, b, t)` | `a + (b - a) * t`; t outside 0…1 extrapolates |

Division/remainder by zero, a square root of a negative value, an invalid logarithm base or any other nonfinite result is an error.

### Angles and trigonometry

| Functions | Purpose |
| --- | --- |
| `sin(x)`, `cos(x)`, `tan(x)` | Trigonometric functions, x in radians |
| `asin(x)`, `acos(x)`, `atan(x)` | Inverse functions, result in radians |
| `atan2(y, x)` | Vector angle with quadrant awareness |
| `sinh(x)`, `cosh(x)`, `tanh(x)` | Hyperbolic functions |
| `rad(degrees)`, `deg(radians)` | Degrees to radians and radians to degrees |

Examples: `sin(rad(30))` ≈ `0.5`, `deg(atan2(1, 1))` ≈ `45`.

### Lists

| Function | Result / example |
| --- | --- |
| `len(list)` | Entry count: `len([2, 4, 6])` → `3` |
| `at(list, index)` | Entry at an index: `at([2, 4, 6], 2)` → `4` |
| `sum(list)` | Sum: `sum([2, 4, 6])` → `12`; empty list → `0` |
| `mean(list)` | Arithmetic mean of a nonempty list |
| `min(list)`, `max(list)` | Minimum/maximum of a nonempty list |
| `range(start, end[, step])` | Numeric sequence, inclusive of the end only if a step reaches it; step cannot be zero |

Examples: `range(1, 5)` → `[1, 2, 3, 4, 5]`; `range(0, 1, 0.25)` → `[0, 0.25, 0.5, 0.75, 1]`. Specify a negative step for a descending range: `range(5, 1, -1)`. More than 4096 entries is an error.

### Numerical calculus

`diff(expression, x, point)` approximates a derivative with respect to the local variable x at a point. It uses a central difference with `h = 1e-5 * max(1, abs(point))`.

Example: `diff(x^3, x, 2)` ≈ `12`.

`integrate(expression, x, lower, upper[, steps])` approximates a definite integral using Simpson's rule. The default is 64 steps; an explicit step count must be an even integer from 2 to 256.

Examples: `integrate(x^2, x, 0, 1)` ≈ `0.3333333333`; `integrate(sin(x), x, 0, pi, 128)` ≈ `2`.

The second argument is a local variable name (`x` or `$x`). Within the first argument it applies only inside this call and does not require a project declaration. Other variables work normally: `integrate($scale * x^2, x, 0, 1)`. Nested calls are allowed within evaluation limits.

These are numerical approximations, not symbolic algebra. Discontinuities, singularities, sharp oscillations and very large/small scales may produce inaccurate results or errors. For these cases, adjust the bounds and step count; do not treat the answer as an exact proof.

## Event variables

These are available when block expressions run and cannot be used in project variables' initial expressions:

| Name | Value |
| --- | --- |
| `$speed` | Speed from the current event context |
| `$hp` | Health from the current event context |
| `$damage` | Damage from the current event context |
| `$repeat_index` | Current repeat number starting at 1; 0 outside a loop |

The event determines the available context and its meaning; an expression does not inspect arbitrary objects by itself. `$speed`, `$hp` and `$damage` use 0 when the required context or existing object is unavailable. Use a relevant numeric block parameter for in-game damage, speed or time, for example `clamp($damage * 1.2, 0, 500)`.

## Editing a variable with an action

In an Edit variable action, select an existing variable:

| Operation | Effect |
| --- | --- |
| Set value / Replace entire list | Replace the value with the expression result. For lists: `[10, 20, 30]` or `range(1, 12)`. |
| Add value | Add the result to a numeric variable. |
| Multiply by value | Multiply a numeric variable by the result. |
| Append item | Add one number to the end of a list. |
| Edit item at index | Replace the number at an existing integer index starting at 1. |
| Remove item at index | Remove an existing integer index; later indices shift. |
| Clear list | Set an empty list `[]`. |

The expression and index are evaluated when the action runs. An empty index field means 1. Result types are checked; integer results are truncated toward zero and checked for overflow. Add and Multiply do not work on lists. The older Set variable, Change variable by and Multiply variable by actions retain their roles.

For example, an Integer variable `shots` with initial expression `0` and an Add value action with `1` counts action executions. With a List variable `salvo` initialized to `range(1, 12)`, Edit item at index `3` with `at($salvo, 3) * 2` changes only its third entry.

List conditions support only equality and inequality: length and every entry are compared. Greater-than and less-than comparisons are unsupported for lists. To compare length or sum, first assign `len($salvo)` or `sum($salvo)` to a numeric helper variable using an Edit variable action, then compare that variable in a condition.

## Limits and errors

A project: up to 256 variables. Each expression: up to 4096 characters, expression tree depth 32, and 100000 evaluation operations. A list: up to 4096 numbers. An integral: up to 256 steps. A complex nested expression may exhaust its budget sooner than a short, simple one.

Expressions and variables work with regular scenario events. Projectile impact: fragments uses the game's native mechanism and does not support these blocks; project validation reports the incompatibility.

Read errors beside the variable or in project validation. Common causes include a decimal comma instead of a point, an unknown name, initial-value cycles, incompatible types, index 0, an empty list in `mean`, division by zero, overflow or a limit being exceeded. Fix the expression or dependencies before export.

If an error occurs in Forts, the current chain invocation stops. The failed variable-edit action leaves its target variable unchanged. Earlier completed actions retain their effects: there is no whole-chain rollback. `FMS_FORMULA_ERROR` is written to the log at most once per block per match. A later event or timer can start the chain again.

Changing the interface language preserves project expressions, names and values. Function names and technical references are identical in both languages.
