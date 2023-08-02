let greet = (name) => "Hello " ++ name
let add = (x, y, z) => x + y + z

let main = (args) => {
  Js.log(greet("world!")) // "Hello world!"
  Js.log(add(1, 2, 3)) // 6
}
