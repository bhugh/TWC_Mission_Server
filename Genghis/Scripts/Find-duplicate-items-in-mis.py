pythonimport re

##### FIND DUPLICATE LINES among stationaries of a .mis file - AI first draft

# Replace 'mission_file.mis' with the actual filename of your map
input_file = "mission_file.mis"
output_file = "mission_file_CLEANED.mis"

seen_coordinates = set()
unique_lines = []
duplicate_count = 0

# Regex to isolate the X and Y coordinates (the two decimals following the country tag like 'de' or 'gb')
coord_pattern = re.compile(r"Static\d+\s+\S+\s+\S+\s+(\d+\.\d+)\s+(\d+\.\d+)")

with open(input_file, "r", encoding="utf-8") as f:
    for line in f:
        match = coord_pattern.search(line)
        if match:
            # Grab the X and Y coordinates as a unique pair
            coords = (match.group(1), match.group(2))
            
            if coords in seen_coordinates:
                duplicate_count += 1
                continue  # Skip this line completely (deletes the duplicate)
            else:
                seen_coordinates.add(coords)
                unique_lines.append(line)
        else:
            # Keep all header lines, map settings, and non-object lines intact
            unique_lines.append(line)

with open(output_file, "w", encoding="utf-8") as f:
    f.writelines(unique_lines)

print(f"Success! Removed {duplicate_count} duplicate objects.")
print(f"Cleaned file saved as: {output_file}")