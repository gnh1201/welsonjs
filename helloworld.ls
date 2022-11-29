fortune = require "lib/fortune.ls"

# Easy listing of implicit objects
table1 =
  * id: 1
    name: 'george'
  * id: 2
    name: 'mike'
  * id: 3
    name: 'donald'

table2 =
  * id: 2
    age: 21
  * id: 1
    age: 20
  * id: 3
    age: 26

main = (args) ->
  console.log JSON.stringify(table1)
  console.log JSON.stringify(table2)
  console.log "Hello world, LiveScript"
  fortune.sayFortune "fortune!"

exports.main = main