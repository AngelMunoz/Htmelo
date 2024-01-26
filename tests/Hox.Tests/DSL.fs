module DSL

open System
open System.Threading
open System.Threading.Tasks

open Xunit

open FSharp.Control
open IcedTasks


open Hox
open Hox.Core
open Hox.Rendering

module Elements =
  [<Fact>]
  let ``addToNode can be used to add a child node``() =
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element {
                tag = "div"
                children = [ Text "Hello, World!" ]
                attributes = []
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  [<Fact>]
  let ``addToNode can be used to add a child node to a node with existing children``
    ()
    =
    let node =
      Element {
        tag = "div"
        children = [ Text "Hello, World!" ]
        attributes = []
      }

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element {
                tag = "div"
                children = [ Text "Hello, World!"; Text "Hello, World!" ]
                attributes = []
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can add async nodes``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let child =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)
          return Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->

      let! content' = content CancellationToken.None

      match content' with
      | Element {
                  tag = "div"
                  children = [ Text "Hello, World!" ]
                  attributes = []
                } -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single text node, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addtoNode can add fragment nodes to element``() =
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let child = Fragment [ Text "Hello, World!"; Text "Hello, World!" ]

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Element {
                tag = "div"
                children = [ Text "Hello, World!"; Text "Hello, World!" ]
                attributes = []
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can add taskseqs to elements``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let children =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Hello, World!"
          yield Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, children)

    match node' with
    | Element {
                tag = "div"
                children = [ AsyncSeqNode content ]
                attributes = []
              } ->

      let! content' = TaskSeq.toListAsync content

      match content' with
      | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
      | other -> Assert.Fail $"Expected two text nodes, but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  }

module Fragments =

  [<Fact>]
  let ``addToNode can add a child node``() =
    let node = Fragment [ Text "Hello, World!" ]

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Fragment [ Text "Hello, World!"; Text "Hello, World1!" ] -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Fragment with two children, but got %A{other}"

  [<Fact>]
  let ``addToNode can will preserve the order of children when adding another fragment``
    ()
    =
    let node = Fragment [ Text "Hello, World!" ]

    let child = Fragment [ Text "Hello, World1!"; Text "Hello, World2!" ]

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Fragment [ Text "Hello, World!"
                 Text "Hello, World1!"
                 Text "Hello, World2!" ] -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Fragment with three children, but got %A{other}"

  [<Fact>]
  let ``addToNode will preserve the order of children when adding an async node seq``
    ()
    =
    taskUnit {
      let node = Fragment [ Text "Hello, World!" ]

      let child =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            yield Text "Hello, World1!"
            yield Text "Hello, World2!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
          ()
        | other ->
          Assert.Fail
            $"Expected nodes to have three text nodes, but got %A{other}"

      | other ->
        Assert.Fail
          $"Expected node to be a Fragment with three children, but got %A{other}"
    }

module Texts =

  [<Fact>]
  let ``addNode will merge two text nodes``() =
    let node = Text "Hello, World!"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Text "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Text node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addNode will merge two raw nodes``() =
    let node = Raw "<div>Hello, World!</div>"

    let child = Raw "<div>Hello, World1!</div>"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Raw "<div>Hello, World!</div><div>Hello, World1!</div>" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value '<div>Hello, World!</div><div>Hello, World1!</div>', but got %A{other}"

  [<Fact>]
  let ``addNode will not merge a child raw node with a parent text node``() =
    let node = Text "Hello, World!"

    let child = Raw "<div>Hello, World1!</div>"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Text "Hello, World!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value 'Hello, World!<div>Hello, World1!</div>', but got %A{other}"

  [<Fact>]
  let ``addNode will merge raw nodes and text nodes together``() =
    let node = Raw "<div>Hello, World!</div>"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Raw "<div>Hello, World!</div>Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value '<div>Hello, World!</div>Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addToNode will merge two comment nodes together``() =
    let node = Comment "Hello, World!"

    let child = Comment "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Comment "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Comment node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``addToNode will merge mixed text and comment nodes together``() =
    let node = Comment "Hello, World!"

    let child = Text "Hello, World1!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | Comment "Hello, World!Hello, World1!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Raw node with value 'Hello, World!Hello, World1!', but got %A{other}"

  [<Fact>]
  let ``h parses the first string and the subsecuent ones become text nodes``
    ()
    =
    let node = h("p", "Hello, World!", "Hello, World1!")

    match node with
    | Element {
                tag = "p"
                children = [ Text "Hello, World!"; Text "Hello, World1!" ]
                attributes = []
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'p' and two children, but got %A{other}"

module AsyncNodes =

  [<Fact>]
  let ``addToNode can add sync nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = []
              attributes = []
            }
        }
      )

    let child = Text "Hello, World!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element {
                  tag = "div"
                  children = [ Text "Hello, World!" ]
                  attributes = []
                } -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add async nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = []
              attributes = []
            }
        }
      )

    let child =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element {
                  tag = "div"
                  children = [ Text "Hello, World!" ]
                  attributes = []
                } -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add fragment nodes to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = []
              attributes = []
            }
        }
      )

    let child = Fragment [ Text "Hello, World!"; Text "Hello, World!" ]

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element {
                  tag = "div"
                  children = [ Text "Hello, World!"; Text "Hello, World!" ]
                  attributes = []
                } -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and two children, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode can add taskseqs to async nodes``() = taskUnit {
    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = []
              attributes = []
            }
        }
      )

    let children =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Hello, World!"
          yield Text "Hello, World!"
        }
      )

    let node' = NodeOps.addToNode(node, children)

    match node' with
    | AsyncNode content ->
      let! content = content CancellationToken.None

      match content with
      | Element {
                  tag = "div"
                  children = [ AsyncSeqNode nodes ]
                  attributes = []
                } ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"; Text "Hello, World!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have two text nodes, but got %A{other}"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' a single async seq node, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

module AsyncSeqNodes =

  [<Fact>]
  let ``addToNode can add sync nodes to async seq nodes``() = taskUnit {
    let node =
      AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          Text "Hello, World!"
          Text "Hello, World1!"
        }
      )

    let child = Text "Hello, World2!"

    let node' = NodeOps.addToNode(node, child)

    match node' with
    | AsyncSeqNode nodes ->
      let! nodes = TaskSeq.toListAsync nodes

      match nodes with
      | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
        ()
      | other ->
        Assert.Fail
          $"Expected nodes to have three text nodes, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
  }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with a fragment``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child = Fragment [ Text "Hello, World2!"; Text "Hello, World3!" ]

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"
            Text "Hello, World1!"
            Text "Hello, World2!"
            Text "Hello, World3!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have four text nodes, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
    }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with another async seq node``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World2!"
            Text "Hello, World3!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncSeqNode nodes ->
        let! nodes = TaskSeq.toListAsync nodes

        match nodes with
        | [ Text "Hello, World!"
            Text "Hello, World1!"
            Text "Hello, World2!"
            Text "Hello, World3!" ] -> ()
        | other ->
          Assert.Fail
            $"Expected nodes to have four text nodes, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async seq node, but got %A{other}"
    }

  [<Fact>]
  let ``addToNode will respect order when merging an async seq node with an async node``
    ()
    =
    taskUnit {
      let node =
        AsyncSeqNode(
          taskSeq {
            do! Task.Delay(5)
            Text "Hello, World!"
            Text "Hello, World1!"
          }
        )

      let child =
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)
            return Text "Hello, World2!"
          }
        )

      let node' = NodeOps.addToNode(node, child)

      match node' with
      | AsyncNode content ->

        let! content = content CancellationToken.None

        match content with
        | AsyncSeqNode nodes ->
          let! nodes = TaskSeq.toListAsync nodes

          match nodes with
          | [ Text "Hello, World!"; Text "Hello, World1!"; Text "Hello, World2!" ] ->
            ()
          | other ->
            Assert.Fail
              $"Expected nodes to have three text nodes, but got %A{other}"
        | other ->
          Assert.Fail
            $"Expected node to be an async seq node, but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async node, but got %A{other}"
    }

open NodeOps.Operators

[<Fact>]
let ``addToNode will add correctly and every kind of Node into an element parent``
  ()
  =
  taskUnit {
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let node =
      node
      <+ Text "Text Node"
      <+ Raw "<div>Raw Node</div>"
      <+ Comment "Comment Node"
      <+ Fragment [ Text "Fragment Node"; Text "Fragment Node1" ]
      <+ AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)
          return Text "Async Node"
        }
      )
      <+ AsyncSeqNode(
        taskSeq {
          do! Task.Delay(5)
          yield Text "Async Seq Node"
          yield Text "Async Seq Node1"
        }
      )

    match node with
    | AsyncNode node ->
      let! node = node CancellationToken.None

      match node with
      | Element {
                  tag = "div"
                  children = [ Text "Text Node"
                               Raw "<div>Raw Node</div>"
                               Comment "Comment Node"
                               Text "Fragment Node"
                               Text "Fragment Node1"
                               Text "Async Node"
                               AsyncSeqNode asyncSeqChild ]
                  attributes = []
                } ->

        let! asyncSeqChild = TaskSeq.toListAsync asyncSeqChild

        match asyncSeqChild with
        | [ Text "Async Seq Node"; Text "Async Seq Node1" ] -> ()
        | other ->
          Assert.Fail
            $"Expected async seq child to have two text nodes, but got %A{other}"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and six children, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }


module Attributes =

  [<Fact>]
  let ``addAttribute will add an attribute to an element node``() =
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | Element {
                tag = "div"
                children = []
                attributes = [ Attribute {
                                           name = "class"
                                           value = "test-class"
                                         } ]
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"

  [<Fact>]
  let ``addAttribute will add an async attribute to an attribute``() = taskUnit {
    let node =
      Element {
        tag = "div"
        children = []
        attributes = []
      }

    let node' =
      NodeOps.addAttribute(
        node,
        AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)
            return { name = "class"; value = "test-class" }
          }
        )
      )

    match node' with
    | Element {
                tag = "div"
                children = []
                attributes = [ AsyncAttribute attr ]
              } ->
      let! attr = attr CancellationToken.None

      match attr with
      | { name = "class"; value = "test-class" } -> ()
      | other ->
        Assert.Fail
          $"Expected attribute to be an Attribute with name 'class' and value 'test-class', but got %A{other}"
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
  }

  [<Fact>]
  let ``addAttribute will add an attribute to an async node element``() = taskUnit {

    let node =
      AsyncNode(
        cancellableValueTask {
          do! Task.Delay(5)

          return
            Element {
              tag = "div"
              children = []
              attributes = []
            }
        }
      )

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | AsyncNode node ->
      let! node = node CancellationToken.None

      match node with
      | Element {
                  tag = "div"
                  children = []
                  attributes = [ Attribute {
                                             name = "class"
                                             value = "test-class"
                                           } ]
                } -> ()
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    | other ->
      Assert.Fail $"Expected node to be an async node, but got %A{other}"
  }

  [<Fact>]
  let ``addAttribute will ignore any node that is not an element``() =
    let node = Text "Hello, World!"

    let node' =
      NodeOps.addAttribute(
        node,
        Attribute { name = "class"; value = "test-class" }
      )

    match node' with
    | Text "Hello, World!" -> ()
    | other ->
      Assert.Fail
        $"Expected node to be a Text node with value 'Hello, World!', but got %A{other}"

  [<Fact>]
  let ``addAttribute will ignore any node that is not an element, even if it is an async node``
    ()
    =
    taskUnit {
      let node =
        AsyncNode(
          cancellableValueTask {
            do! Task.Delay(5)

            return Text "Hello, World!"
          }
        )

      let node' =
        NodeOps.addAttribute(
          node,
          Attribute { name = "class"; value = "test-class" }
        )

      match node' with
      | AsyncNode node ->
        let! node = node CancellationToken.None

        match node with
        | Text "Hello, World!" -> ()
        | other ->
          Assert.Fail
            $"Expected node to be a Text node with value 'Hello, World!', but got %A{other}"
      | other ->
        Assert.Fail $"Expected node to be an async node, but got %A{other}"
    }

  [<Fact>]
  let ``addAttribute will respect the order of addition of the attributes for both sync/async``
    ()
    =
    taskUnit {

      let node =
        Element {
          tag = "div"
          children = []
          attributes = []
        }

      let node =
        node
        <+. Attribute { name = "class"; value = "test-class" }
        <+. Attribute { name = "id"; value = "id-attr" }
        <+. Attribute {
          name = "data-name"
          value = "data-name-attr"
        }
        <+. AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)

            return {
              name = "my-async-attr"
              value = "my async attr"
            }
          }
        )
        <+. AsyncAttribute(
          cancellableValueTask {
            do! Task.Delay(5)

            return {
              name = "my-async-attr1"
              value = "my async attr1"
            }
          }
        )

      match node with
      | Element {
                  tag = "div"
                  children = []
                  attributes = [ Attribute {
                                             name = "class"
                                             value = "test-class"
                                           }
                                 Attribute { name = "id"; value = "id-attr" }
                                 Attribute {
                                             name = "data-name"
                                             value = "data-name-attr"
                                           }
                                 AsyncAttribute attr
                                 AsyncAttribute attr1 ]
                } ->
        let! attr = attr CancellationToken.None
        let! attr1 = attr1 CancellationToken.None

        match attr, attr1 with
        | {
            name = "my-async-attr"
            value = "my async attr"
          },
          {
            name = "my-async-attr1"
            value = "my async attr1"
          } -> ()
        | other ->
          Assert.Fail
            $"Expected attributes to be an Attribute with name 'my-async-attr' and value 'my async attr' and an Attribute with name 'my-async-attr1' and value 'my async attr1', but got %A{other}"
      | other ->
        Assert.Fail
          $"Expected node to be an Element with tag 'div' and a single child, but got %A{other}"
    }


module DSD =

  [<Fact>]
  let ``sh can produce a function that will create a template``() =
    let template = sh("x-my-tag", h "slot")

    let templatedResult = template(text "Hello, World!")

    match templatedResult with
    | Element {
                tag = "x-my-tag"
                attributes = []
                children = [ Element {
                                       tag = "template"
                                       attributes = [ Attribute {
                                                                  name = "shadowrootmode"
                                                                  value = "open"
                                                                } ]
                                       children = [ Element {
                                                              tag = "slot"
                                                              children = []
                                                              attributes = []
                                                            } ]
                                     }
                             Text "Hello, World!" ]
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'x-my-tag' and a single child, but got %A{other}"

  [<Fact>]
  let ``shcs can produce a function that will create a template``() =
    let template = shcs("x-my-tag", h "slot")

    let templatedResult = template.Invoke(text "Hello, World!")

    match templatedResult with
    | Element {
                tag = "x-my-tag"
                attributes = []
                children = [ Element {
                                       tag = "template"
                                       attributes = [ Attribute {
                                                                  name = "shadowrootmode"
                                                                  value = "open"
                                                                } ]
                                       children = [ Element {
                                                              tag = "slot"
                                                              children = []
                                                              attributes = []
                                                            } ]
                                     }
                             Text "Hello, World!" ]
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'x-my-tag' and a single child, but got %A{other}"

  [<Fact>]
  let ``Scopable Article will have DSD enabled``() =
    let node =
      ScopableElements.article(
        h "link[href=https://some-css-file]",
        text "Hello, World!"
      )

    match node with
    | Element {
                tag = "article"
                attributes = []
                children = [ Element {
                                       tag = "template"
                                       attributes = [ Attribute {
                                                                  name = "shadowrootmode"
                                                                  value = "open"
                                                                } ]
                                       children = [ Element {
                                                              tag = "link"
                                                              attributes = [ Attribute {
                                                                                         name = "href"
                                                                                         value = "https://some-css-file"
                                                                                       } ]
                                                              children = []
                                                            } ]
                                     }
                             Fragment [ Text "Hello, World!" ] ]
              } -> ()
    | other ->
      Assert.Fail
        $"Expected node to be an Element with tag 'article' and a single child, but got %A{other}"
