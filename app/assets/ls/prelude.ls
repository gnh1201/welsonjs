apply = (f, list) -->
  f.apply null, list

curry = (f) ->
  curry$ f # using util method curry$ from livescript

flip = (f, x, y) --> f y, x

fix = (f) ->
  ( (g) -> -> f(g g) ...arguments ) do
    (g) -> -> f(g g) ...arguments

over = (f, g, x, y) --> f (g x), (g y)

memoize = (f) ->
  memo = {}
  (...args) ->
    key = [arg + typeof! arg for arg in args].join ''
    memo[key] = if key of memo then memo[key] else f ...args

#? wrap

each = (f, xs) -->
  for x in xs
    f x
  xs

map = (f, xs) -->
  [f x for x in xs]

compact = (xs) -->
  [x for x in xs when x]

filter = (f, xs) -->
  [x for x in xs when f x]

reject = (f, xs) -->
  [x for x in xs when not f x]

remove = (el, xs) -->
  i = elem-index el, xs
  xs.slice!
    ..splice i, 1 if i?

partition = (f, xs) -->
  passed = []
  failed = []
  for x in xs
    (if f x then passed else failed).push x
  [passed, failed]

find = (f, xs) -->
  for x in xs when f x
    return x
  void

head = first = (xs) ->
  xs.0

tail = (xs) ->
  return void unless xs.length
  xs.slice 1

last = (xs) ->
  xs[*-1]

initial = (xs) ->
  return void unless xs.length
  xs.slice 0, -1

empty = (xs) ->
  not xs.length

reverse = (xs) ->
  xs.concat!.reverse!

unique = (xs) ->
  result = []
  for x in xs when x not in result
    result.push x
  result

unique-by = (f, xs) -->
  seen = []
  for x in xs
    val = f x
    continue if val in seen
    seen.push val
    x

fold = foldl = (f, memo, xs) -->
  for x in xs
    memo = f memo, x
  memo

fold1 = foldl1 = (f, xs) -->
  fold f, xs.0, xs.slice 1

foldr = (f, memo, xs) -->
  for x in xs by -1
    memo = f x, memo
  memo

foldr1 = (f, xs) -->
  foldr f, xs[*-1], xs.slice 0, -1

unfoldr = (f, b) -->
  result = []
  x = b
  while (f x)?
    result.push that.0
    x = that.1
  result

concat = (xss) ->
  [].concat.apply [], xss

concat-map = (f, xs) -->
  [].concat.apply [], [f x for x in xs]

flatten = (xs) -->
  [].concat.apply [], [(if typeof! x is 'Array' then flatten x else x) for x in xs]

difference = (xs, ...yss) ->
  results = []
  :outer for x in xs
    for ys in yss
      continue outer if x in ys
    results.push x
  results

intersection = (xs, ...yss) ->
  results = []
  :outer for x in xs
    for ys in yss
      continue outer unless x in ys
    results.push x
  results

union = (...xss) ->
  results = []
  for xs in xss
    for x in xs
      results.push x unless x in results
  results

count-by = (f, xs) -->
  results = {}
  for x in xs
    key = f x
    if key of results
      results[key] += 1
    else
      results[key] = 1
  results

group-by = (f, xs) -->
  results = {}
  for x in xs
    key = f x
    if key of results
      results[key].push x
    else
      results[key] = [x]
  results

and-list = (xs) ->
  for x in xs when not x
    return false
  true

or-list = (xs) ->
  for x in xs when x
    return true
  false

any = (f, xs) -->
  for x in xs when f x
    return true
  false

all = (f, xs) -->
  for x in xs when not f x
    return false
  true

sort = (xs) ->
  xs.concat!.sort (x, y) ->
    if x > y
      1
    else if x < y
      -1
    else
      0

sort-with = (f, xs) -->
  xs.concat!.sort f

sort-by = (f, xs) -->
  xs.concat!.sort (x, y) ->
    if (f x) > (f y)
      1
    else if (f x) < (f y)
      -1
    else
      0

sum = (xs) ->
  result = 0
  for x in xs
    result += x
  result

product = (xs) ->
  result = 1
  for x in xs
    result *= x
  result

mean = average = (xs) ->
  sum = 0
  for x in xs
    sum += x
  sum / xs.length

maximum = (xs) ->
  max = xs.0
  for x in xs.slice 1 when x > max
    max = x
  max

minimum = (xs) ->
  min = xs.0
  for x in xs.slice 1 when x < min
    min = x
  min

maximum-by = (f, xs) -->
  max = xs.0
  for x in xs.slice 1 when (f x) > (f max)
    max = x
  max

minimum-by = (f, xs) -->
  min = xs.0
  for x in xs.slice 1 when (f x) < (f min)
    min = x
  min

scan = scanl = (f, memo, xs) -->
  last = memo
  [memo] ++ [last = f last, x for x in xs]

scan1 = scanl1 = (f, xs) -->
  return void unless xs.length
  scan f, xs.0, xs.slice 1

scanr = (f, memo, xs) -->
  xs = xs.concat!.reverse!
  (scan f, memo, xs).reverse!

scanr1 = (f, xs) -->
  return void unless xs.length
  xs = xs.concat!.reverse!
  (scan f, xs.0, xs.slice 1).reverse!

slice = (x, y, xs) -->
  xs.slice x, y

take = (n, xs) -->
  if n <= 0
    xs.slice 0, 0
  else
    xs.slice 0, n

drop = (n, xs) -->
  if n <= 0
    xs
  else
    xs.slice n

split-at = (n, xs) --> [(take n, xs), (drop n, xs)]

take-while = (p, xs) -->
  len = xs.length
  return xs unless len
  i = 0
  while i < len and p xs[i]
    i += 1
  xs.slice 0 i

drop-while = (p, xs) -->
  len = xs.length
  return xs unless len
  i = 0
  while i < len and p xs[i]
    i += 1
  xs.slice i

span = (p, xs) --> [(take-while p, xs), (drop-while p, xs)]

break-list = (p, xs) --> span (not) << p, xs

zip = (xs, ys) -->
  result = []
  len = ys.length
  for x, i in xs
    break if i is len
    result.push [x, ys[i]]
  result

zip-with = (f, xs, ys) -->
  result = []
  len = ys.length
  for x, i in xs
    break if i is len
    result.push f x, ys[i]
  result

zip-all = (...xss) ->
  min-length = undefined
  for xs in xss
    min-length <?= xs.length
  [[xs[i] for xs in xss] for i til min-length]

zip-all-with = (f, ...xss) ->
  min-length = undefined
  for xs in xss
    min-length <?= xs.length
  [f.apply(null, [xs[i] for xs in xss]) for i til min-length]

at = (n, xs) -->
  if n < 0 then xs[xs.length + n] else xs[n]

elem-index = (el, xs) -->
  for x, i in xs when x is el
    return i
  void

elem-indices = (el, xs) -->
  [i for x, i in xs when x is el]

find-index = (f, xs) -->
  for x, i in xs when f x
    return i
  void

find-indices = (f, xs) -->
  [i for x, i in xs when f x]

values = (object) ->
  [x for , x of object]

keys = (object) ->
  [x for x of object]

pairs-to-obj= (object) ->
  {[x.0, x.1] for x in object}

obj-to-pairs = (object) ->
  [[key, value] for key, value of object]

lists-to-obj = (keys, values) -->
  {[key, values[i]] for key, i in keys}

obj-to-lists = (object) ->
  keys = []
  values = []
  for key, value of object
    keys.push key
    values.push value
  [keys, values]

empty = (object) ->
  for x of object then return false
  true

each = (f, object) -->
  for , x of object then f x
  object

map = (f, object) -->
  {[k, f x] for k, x of object}

compact = (object) -->
  {[k, x] for k, x of object when x}

filter = (f, object) -->
  {[k, x] for k, x of object when f x}

reject = (f, object) -->
  {[k, x] for k, x of object when not f x}

partition = (f, object) -->
  passed = {}
  failed = {}
  for k, x of object
    (if f x then passed else failed)[k] = x
  [passed, failed]

find = (f, object) -->
  for , x of object when f x then return x
  void

split = (sep, str) -->
  str.split sep

join = (sep, xs) -->
  xs.join sep

lines = (str) ->
  return [] unless str.length
  str.split '\n'

unlines = (.join '\n')

words = (str) ->
  return [] unless str.length
  str.split /[ ]+/

unwords = (.join ' ')

chars = (.split '')

unchars = (.join '')

reverse = (str) ->
  str.split '' .reverse!.join ''

repeat = (n, str) -->
  result = ''
  for til n
    result += str
  result

capitalize = (str) ->
  (str.char-at 0).to-upper-case! + str.slice 1

camelize = (.replace /[-_]+(.)?/g, (, c) -> (c ? '').to-upper-case!)

# convert camelCase to camel-case, and setJSON to set-JSON
dasherize = (str) ->
    str
      .replace /([^-A-Z])([A-Z]+)/g, (, lower, upper) ->
         "#{lower}-#{if upper.length > 1 then upper else upper.to-lower-case!}"
      .replace /^([A-Z]+)/, (, upper) ->
         if upper.length > 1 then "#upper-" else upper.to-lower-case!

max = (>?)

min = (<?)

negate = (x) -> -x

abs = Math.abs

signum = (x) ->
  if x < 0
    -1
  else if x > 0
    1
  else
    0

quot = (x, y) --> ~~(x / y)

rem = (%)

div = (x, y) --> Math.floor x / y

mod = (%%)

recip = (1 /)

pi = Math.PI

tau = pi * 2

exp = Math.exp

sqrt = Math.sqrt

ln = Math.log

pow = (^)

sin = Math.sin

tan = Math.tan

cos = Math.cos

asin = Math.asin

acos = Math.acos

atan = Math.atan

atan2 = (x, y) --> Math.atan2 x, y

truncate = (x) -> ~~x

round = Math.round

ceiling = Math.ceil

floor = Math.floor

is-it-NaN = (x) -> x isnt x

even = (x) -> x % 2 == 0

odd = (x) -> x % 2 != 0

gcd = (x, y) -->
  x = Math.abs x
  y = Math.abs y
  until y is 0
    z = x % y
    x = y
    y = z
  x

lcm = (x, y) -->
  Math.abs Math.floor (x / (gcd x, y) * y)