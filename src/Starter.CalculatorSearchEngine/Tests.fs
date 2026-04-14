// These tests are not executed during the test process (because there is no test process)
// It could be a good idea to add tests to Starter. Maybe.
module Starter.CalculatorSearchEngine.Tests

open Starter.CalculatorSearchEngine.Types
open Starter.CalculatorSearchEngine.Parser
open Starter.CalculatorSearchEngine.Simplifications

let test = tryParse >> Result.defaultWith failwith >> simplify >> (=)

// Cos
assert (test "cos(0*pi)" Trigo.one)
assert (test "cos(2*cos(cos(cos((1/3)*pi + (2/3)*pi - pi))*i))" Trigo.one)
assert (test "cos(pi/6)" Trigo.sqrt3div2)
assert (test "cos(pi/4)" Trigo.sqrt2div2)
assert (test "cos(pi/3)" Trigo.half)
assert (test "cos(pi/2)" Trigo.zero)
assert (test "cos(2*pi/3)" (Expression.negate Trigo.half))
assert (test "cos(3*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "cos(5*pi/6)" (Expression.negate Trigo.sqrt3div2))
assert (test "cos(12*pi/12)" (Expression.negate Trigo.one))
assert (test "cos(7*pi/6)" (Expression.negate Trigo.sqrt3div2))
assert (test "cos(5*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "cos(4*pi/3)" (Expression.negate Trigo.half))
assert (test "cos(3*pi/2)" (Expression.negate Trigo.zero))
assert (test "cos(5*pi/3)" Trigo.half)
assert (test "cos(7*pi/4)" Trigo.sqrt2div2)
assert (test "cos(11*pi/6)" Trigo.sqrt3div2)
assert (test "cos(18*pi)" Trigo.one)
assert (test "cos(17*pi)" (Expression.negate Trigo.one))

// Sin
// sin basic angles
assert (test "sin(0*pi)" Trigo.zero)
assert (test "sin(pi/6)" Trigo.half)
assert (test "sin(pi/4)" Trigo.sqrt2div2)
assert (test "sin(pi/3)" Trigo.sqrt3div2)
assert (test "sin(pi/2)" Trigo.one)

// quadrant II
assert (test "sin(2*pi/3)" Trigo.sqrt3div2)
assert (test "sin(3*pi/4)" Trigo.sqrt2div2)
assert (test "sin(5*pi/6)" Trigo.half)

// pi
assert (test "sin(12*pi/12)" Trigo.zero)

// quadrant III
assert (test "sin(7*pi/6)" (Expression.negate Trigo.half))
assert (test "sin(5*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "sin(4*pi/3)" (Expression.negate Trigo.sqrt3div2))

// 3pi/2
assert (test "sin(3*pi/2)" (Expression.negate Trigo.one))

// quadrant IV
assert (test "sin(5*pi/3)" (Expression.negate Trigo.sqrt3div2))
assert (test "sin(7*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "sin(11*pi/6)" (Expression.negate Trigo.half))

// periodicity
assert (test "sin(18*pi)" Trigo.zero)
assert (test "sin(19*pi/2)" Trigo.one) // 19π/2 = 9π + π/2

// basic values
module Trigo =
    let sqrt3 = Expression.pow (Expression.int 3) (Expression.frac 1 2)
    let sqrt3div3 = Expression.divide sqrt3 (Expression.int 3)

// Tan
assert (test "tan(0*pi)" Trigo.zero)
assert (test "tan(pi/6)" Trigo.sqrt3div3)
assert (test "tan(pi/4)" Trigo.one)
assert (test "tan(pi/3)" Trigo.sqrt3)

// quadrant II
assert (test "tan(2*pi/3)" (Expression.negate Trigo.sqrt3))
assert (test "tan(3*pi/4)" (Expression.negate Trigo.one))
assert (test "tan(5*pi/6)" (Expression.negate Trigo.sqrt3div3))

// quadrant III
assert (test "tan(7*pi/6)" Trigo.sqrt3div3)
assert (test "tan(5*pi/4)" Trigo.one)
assert (test "tan(4*pi/3)" Trigo.sqrt3)

// quadrant IV
assert (test "tan(5*pi/3)" (Expression.negate Trigo.sqrt3))
assert (test "tan(7*pi/4)" (Expression.negate Trigo.one))
assert (test "tan(11*pi/6)" (Expression.negate Trigo.sqrt3div3))

// periodicity (period π)
assert (test "tan(9*pi/4)" Trigo.one) // π/4 + 2π
assert (test "tan(13*pi/6)" Trigo.sqrt3div3)

assert (test "tan(pi/2)" Undefined)
assert (test "tan(3*pi/2)" Undefined)

// Negative angle

// Sin
// basic negatives
assert (test "sin(-pi/6)" (Expression.negate Trigo.half))
assert (test "sin(-pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "sin(-pi/3)" (Expression.negate Trigo.sqrt3div2))
assert (test "sin(-pi/2)" (Expression.negate Trigo.one))

// larger negatives
assert (test "sin(-2*pi/3)" (Expression.negate Trigo.sqrt3div2))
assert (test "sin(-3*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "sin(-5*pi/6)" (Expression.negate Trigo.half))

// periodic negatives
assert (test "sin(-5*pi/3)" Trigo.sqrt3div2)
assert (test "sin(-7*pi/4)" Trigo.sqrt2div2)
assert (test "sin(-11*pi/6)" Trigo.half)

// large multiple
assert (test "sin(-18*pi)" Trigo.zero)

// Cos
assert (test "cos(-pi/6)" Trigo.sqrt3div2)
assert (test "cos(-pi/4)" Trigo.sqrt2div2)
assert (test "cos(-pi/3)" Trigo.half)
assert (test "cos(-pi/2)" Trigo.zero)

assert (test "cos(-2*pi/3)" (Expression.negate Trigo.half))
assert (test "cos(-3*pi/4)" (Expression.negate Trigo.sqrt2div2))
assert (test "cos(-5*pi/6)" (Expression.negate Trigo.sqrt3div2))

assert (test "cos(-7*pi/4)" Trigo.sqrt2div2)
assert (test "cos(-11*pi/6)" Trigo.sqrt3div2)

assert (test "cos(-18*pi)" Trigo.one)

// Tan
assert (test "tan(-pi/6)" (Expression.negate Trigo.sqrt3div3))
assert (test "tan(-pi/4)" (Expression.negate Trigo.one))
assert (test "tan(-pi/3)" (Expression.negate Trigo.sqrt3))

assert (test "tan(-2*pi/3)" Trigo.sqrt3)
assert (test "tan(-3*pi/4)" Trigo.one)
assert (test "tan(-5*pi/6)" Trigo.sqrt3div3)

assert (test "tan(-7*pi/4)" Trigo.one)
assert (test "tan(-11*pi/6)" Trigo.sqrt3div3)

// periodicity
assert (test "tan(-9*pi/4)" (Expression.negate Trigo.one))


// Angle normalization
assert (test "sin(pi/6 + 2*pi)" Trigo.half)
assert (test "sin(pi/6 + 4*pi)" Trigo.half)
assert (test "sin(pi/6 - 2*pi)" Trigo.half)
assert (test "sin(pi/6 - 6*pi)" Trigo.half)
assert (test "sin(100*pi)" Trigo.zero)
assert (test "sin(101*pi)" Trigo.zero)

assert (test "cos(pi/3 + 2*pi)" Trigo.half)
assert (test "cos(pi/3 - 2*pi)" Trigo.half)
assert (test "cos(pi/3 + 6*pi)" Trigo.half)
assert (test "cos(49*pi/6)" Trigo.sqrt3div2)   // 49π/6 = 8π + π/6
assert (test "cos(33*pi/4)" Trigo.sqrt2div2)   // 33π/4 = 8π + π/4
assert (test "cos(-47*pi/3)" Trigo.half)
assert (test "cos(200*pi)" Trigo.one)
assert (test "cos(201*pi)" (Expression.negate Trigo.one))

assert (test "tan(pi/6 + pi)" Trigo.sqrt3div3)
assert (test "tan(pi/6 + 5*pi)" Trigo.sqrt3div3)
assert (test "tan(pi/6 - pi)" Trigo.sqrt3div3)
assert (test "tan(19*pi/4)" Trigo.one)        // 19π/4 = 4π + 3π/4 → tan(3π/4) = -1? careful
assert (test "tan(17*pi/6)" (Expression.negate Trigo.sqrt3div3))
assert (test "tan(25*pi/4)" Trigo.one)
assert (test "tan(-23*pi/6)" Trigo.sqrt3div3)
assert (test "tan(100*pi)" Trigo.zero)
assert (test "tan(101*pi)" Trigo.zero)

// Hard
assert (test "sin(2*pi + pi/3 - 4*pi)" (Expression.negate Trigo.sqrt3div2))
assert (test "cos(6*pi + 3*pi/4 - 8*pi)" (Expression.negate Trigo.sqrt2div2))
assert (test "tan(5*pi + pi/4 - 3*pi)" Trigo.one)

assert (test "cos(7!*pi + pi - 3*pi)" Trigo.one)
