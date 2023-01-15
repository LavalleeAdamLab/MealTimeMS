#!/bin/bash
let i=0
while :; do
    let i++
    date +%Y-%m-%d -d "$i day ago" >/dev/null 2>&1 || { echo $i && exit 1; }
done
