This README contains a short description of the data, the folder
structure, file naming and some known problems on the
labelling. Possible improvements are proposed in the TODO section.
The data is usable for any purpose, under the Creative Commons
Attribution 4.0 International (CC BY 4.0) licence.  For more details
on the database design and recording, please consult:

Pierre-Edouard Honnet, Alexandros Lazaridis, Philip N. Garner and Junichi Yamagishi, The SIWIS French Speech Synthesis Database – Design and recording of a high quality French database for speech synthesis, Idiap-Internal-RR-06-2017

== Content: ==
* wavs: segmented audio (except part4, unsegmented, and part5 partially
segmented)
* text: prompts corresponding to audio
* lists: recommended training / development / test sets
* labs: aligned HTS labels
* emph_labs: aligned HTS labels with emphasis mark on emphasised word
  (P:0 for neutral words, P:1 for emphasised words)
* other:
  - prompts gathered in big files
  - question file to use in HTS training when using provided labels


== Syntax of file names: ==
* For part 1,2,3 and 5:
  [style]_[type]_[session]_[id].[ext] where:
  - style = neutral (neut) / emphasised (emph) / expressive (expr)
  - type = parliament (parl) / book (book) / siwis (siwis) / sus (sus) /
chapter (chap)
  - session = 2 digits
  - id = 4 digits
  - ext = wav / txt / lab
* part4:
  chap_full.[ext]


== Notes: ==
* Part 1 to 4 are from the same (main) speaker.
* Part 5 is uttered by a different speaker.
* Part 1 to 3 have labels.
* Part 4 and 5 do not have labels because they are not segmented.


== Known problems: ==
* The [s] sounds in "c'est" are often (if not always) replaced by
  [k]. This is a problem in the generation of labels.
* It may happen that some emphasised words are not labelled as
  emphasised in the case of multiple emphasised words in the sentence.


== TODO: ==
* Instead of just labels on known emphasised words, also add
  information about position of emphasised word relatively to the
  current word, for instance.
