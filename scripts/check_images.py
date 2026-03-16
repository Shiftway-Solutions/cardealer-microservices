#!/usr/bin/env python3
"""Check which Hatchback/Minivan image URLs return broken HTTP codes."""
import subprocess
import json

urls = {
    "Chevrolet Spark (92bbcc3c)": "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
    "Mazda 2 (21212b69)": "https://images.unsplash.com/photo-1617469767253-70a026ef7ed5?w=800&q=80",
    "VW Golf (4a6c7909)": "https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=80",
    "Suzuki Swift (1e566f50)": "https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=80",
    "Kia Rio (8853a8bb)": "https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=80",
    "Honda Fit (dec5be80)": "https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=80",
    "Toyota Yaris (b9414d65)": "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=80",
    # Minivans
    "Chevy Spin (4825406d)": "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
    "Mitsubishi Xpander (c9cbeee8)": "https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=80",
    "Toyota Sienna (18be3775)": "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=80",
}

seen_urls = {}
for name, url in urls.items():
    photo_id = url.split("/photo-")[1].split("?")[0]
    r = subprocess.run(
        ["curl", "-sk", "--max-time", "10", "-o", "/dev/null", "-w", "%{http_code}", url],
        capture_output=True, text=True
    )
    code = r.stdout.strip()
    dup = " [SAME URL AS: " + seen_urls[photo_id] + "]" if photo_id in seen_urls else ""
    seen_urls[photo_id] = name
    status = "OK  " if code == "200" else f"BAD ({code})"
    print(f"{status} {name}{dup}")
