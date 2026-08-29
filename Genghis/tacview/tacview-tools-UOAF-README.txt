tacview-filter — given some example.zip.acmi or example.txt.acmi, spits out a example-filtered.zip.acmi, shrunk by:

Delta-encoding all changes to each object
Decimating updates to rates suggested by https://www.tacview.net/documentation/realtime/en/
Remapping all object IDs to smaller numbers.
This can also be used as a traditional Unix-style filter, reading from stdin and writing to stdout.

tacview-server - does all of the above while serving the output in real-time with Tacview or other compatible clients, like OpenRadar.

tacview-replay reads an ACMI file and writes it to stdout at the speed dictated by its timestamps. Mostly used as a test tool for tacview-server.

tacview-stats gathers statistics about the counts and frequencies of different categories of objects in an ACMI file. It's useful to understand what entries take up the most space.