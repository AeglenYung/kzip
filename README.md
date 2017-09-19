# kzip
Command line tool for Zip file on Windows and Ubuntu

## Credit
1. Ionic.Zip.Reduced (v1.9.1.8)
   DotNetZip.Reduced by Dino Chiesa
2. xxHashSharp by Seok-Ju, Yun

## Basic Usage
**Syntax:**
* kzip -cf new.ZIP  [opt ..] [file ..]
* kzip -vf file.ZIP [opt ..]
* kzip -xf file.ZIP [opt ..] [file ..]

**Basic Help:**
* kzip -?

**Command Help:**
* kzip [-c, -v, -x] [-?, -h, --help]
* kzip -c -?
* kzip -v -?
* kzip -x -?

**Version:**
* kzip --version

**Sample:**
* kzip -cf backup.zip readme.txt
* kzip -vf backup.zip
* kzip -x --file=backup.zip

**Switch options:**
* -v, --command-view
* -c, --command-create 
* -x, --command-extract
* -q, --quiet, 
* --md5, Unique by MD5
* --xhash, Unique by xxHash
* --debug

**Value options:**
* -f, --file, =zip-filename

## Create command:
* kzip -cf new.ZIP [opt ..] [file ..]

**Switch options:**
* --ask-password

**Value options:**
* -p, --password 
* --temp-dir
* --level, =1 to 9 (0:Store; 5:Default; 9:Best)
* --encrypt, =[256, 128, weak] (256:Default)
* -T, --list, =listFile (Console if -)

## View command:
* kzip -vf file.zip [opt ..]

**Switch options:**
* -t, --total, Total only
* -s, --sum, Sum by ext

**Value options:**
* --size, =short or comma or kilo
* --count, =short or comma or kilo
* --hide, =size, date, time
* --show, =ratio, crc, encrypt, all
* --sort, =name or date or size

## Extract command:
* kzip -xf file.ZIP [opt ..] [file ..]

**Switch options:**
* --ask-password
* --fix-name, Remove invalid path char
* -n, --new-dir, Create zip-filename for out-dir 

**Value options:**
* -p, --password
* -o, --out-dir, =NewDir

## Config file option:
* --cfg-off, =exe or private or all
* --cfg-save, =exe or private
* --cfg-show

# License
BSD 2-clause license.
