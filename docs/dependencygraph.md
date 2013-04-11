# Rawc dependency graph sorting

The rawc dependency graph is a dependency graph of computation nodes, i.e.
a directed graph connecting each node with the nodes on which its computation
depends. The main purpose of the graph is to request the ordering of a subset
of its nodes from it, ordered such that dependencies of each node are always
computed before the node is computed.

What makes the rawc graph a bit more special than a normal dependency graph is
that nodes can be grouped together based on their embedding. Basically, nodes
that can be computed in a loop are should be grouped together, as long as their
dependencies allow it.

For example, consider the following dependencies:

1. `a(A) : b(A)`
2. `b(A) : c(A), d`
3. `c(A) :`
4. `d    :`

Where `a(A)`, `b(A)` and `c(A)` are part of embedding `A`. The correct ordering would be
`d`, `c(A)`, `b(A)`, `a(A)`. Here `c(A)`, `a(A)` and `a(A)` can be computed in a loop.

It's possible however that nodes belonging to the same embedding cannot be all
grouped together because a dependency separates the groups. For example:

1. `a(A) : b(A)`
2. `b(A) : d`
3. `d    : c(A)`

Here the order would be: `c(A)`, `d`, `b(A)`, `a(A)`. `c(A)` is now separated
from `b(A)` and `a(A)` and they cannot be calculated in the same loop.

The goal of the dependency sorting is twofold. First it should ensure all
nodes are topologically sorted according to their dependencies. Second, nodes
with the same embedding, not separated by dependencies, should be grouped
together.

# Standard topological sort algorithm
A standard algorithm for performing topological sort can be written like this
(Kahn, 1962)

    L: empty result set
    S: set of leaf nodes on which no other nodes depend

    while not(empty(S))
        n = S.dequeue
        L.push(n)

        for each node m with edge e from n to m
            remove e from graph

            if no nodes depend on m anymore
                S.queue(m)

    if edges in graph
        graph is not asyclic

    return L

# Augmented topological sort algorithm
To keep nodes in the same embedding grouped together in the resulting sorted
list, we can change the algorithm above by making S a priority queue where
nodes are sorted based on their embedding.
