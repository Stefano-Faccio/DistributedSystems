# Akka.NET Distributed Database

In this project, we created a DHT-based peer-to-peer key-value storage that
has APIs to get and update data inside. In this document, we will explain
the main algorithms that we came up with to satisfy the given requirements.
The data is stored between nodes and replicated to distribute load and
grant the system is still online if a particular node crashes. Nodes know each
other and where a particular key is stored at all times. Channels are reliable,
so no message is lost while traveling, and nodes do not crash in the middle
of an operation, but they could be unavailable when a new one starts.
The main requisite is sequential consistency, which is an older value
should not be returned after a previous operation returned a newer one.
For some algorithm, we traded efficiency of time and memory to ensure this
requirement.
The project was written with C# and Akka.NET
