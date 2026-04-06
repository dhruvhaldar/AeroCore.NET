import sys

def main():
    with open(".jules/bolt.md", "r") as f:
        content = f.read()

    new_entry = """## 2026-06-12 - CultureInfo Property Lookup Overhead
**Learning:** Accessing context-sensitive properties like `CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]` inside helper methods called multiple times per tick in a high-frequency UI loop causes redundant and expensive property evaluations.
**Action:** Fetch context-sensitive properties locally once per loop tick and pass them down as parameters to helper methods, eliminating redundant lookup overhead.
"""
    if "CultureInfo Property Lookup Overhead" not in content:
        with open(".jules/bolt.md", "a") as f:
            f.write("\n" + new_entry)

if __name__ == "__main__":
    main()
