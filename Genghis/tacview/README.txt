tacview-filter
==============

A command-line tool that reads Tacview ACMI files (2.1 and 2.2), applies
filters, and writes a modified ACMI file. Use it to trim recordings for
debriefing and analysis: time crop, spatial filter, fog-of-war, and
remove/keep by object type or property.

Run from a terminal:

  tacview-filter [options] <input.acmi>
  tacview-filter --help

Output defaults to out.acmi (compressed). Use -o to set the output path and
--text for uncompressed output.

Full documentation, options, and examples:
  https://github.com/syn111/tacview-filter

License: GPL-3.0-or-later. See the repository for details.
