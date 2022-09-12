# A simple script to download the relevant files from the 12y markup repo.
# Note that this isn't a submodule, this is done to better control versioning
# in this blog generator repo
set -e

MBASE="https://raw.githubusercontent.com/12Me21/markup2/cactus"
# MBASE="https://raw.githubusercontent.com/12Me21/markup2/class"
FILES="legacy.js langs.js render.js helpers.js parse.js markup.css"

cd ../blog-generator/WebResources/markup
rm -f *

for f in $FILES
do
    wget "$MBASE/$f"
done 

