set xtics 1; 
set xlabel "Number of sites"
set ylabel "Number of messages"
set yrange [0:]
set grid ls 0
set term aqua title "Sites vs messages"; set terminal postscript eps enhanced color
set output "./img/sites_vs_messages.eps"
plot "./raft_sites_vs_messages.txt" with line title "Lets3D-C" lc rgb "green" lw 3,         "./chainvoxel_sites_vs_messages.txt" with line title "ChainVoxel" lc rgb "red" lw 3
