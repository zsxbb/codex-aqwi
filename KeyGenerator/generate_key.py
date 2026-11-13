import sys

def generate_key(username: str) -> str:
    if not username:
        return "00000"
    
    # Pastikan sama persis dengan logika game: trim, lowercase, dan tambahkan secret salt
    input_str = username.strip().lower() + "codex_secret_2026"
    
    # FNV-1a 32-bit hash algorithm
    fnv_offset_basis = 2166136261
    fnv_prime = 16777619
    
    hash_val = fnv_offset_basis
    for char in input_str:
        # XOR dengan kode integer karakter
        hash_val ^= ord(char)
        # Kalikan dengan FNV prime dan mask (& 0xFFFFFFFF) untuk membatasi ke 32-bit unsigned int
        hash_val = (hash_val * fnv_prime) & 0xFFFFFFFF
        
    # Format ke kode 5-digit (10000 - 99999)
    code = (hash_val % 90000) + 10000
    return str(code)

if __name__ == "__main__":
    print("=================================================")
    print("        Codex Activation Key Generator        ")
    print("=================================================")
    print("Secret Salt: codex_secret_2026\n")
    
    while True:
        try:
            username = input("Enter username (or type 'exit' to quit): ").strip()
            if not username:
                print("Username cannot be empty!\n")
                continue
            if username.lower() == 'exit':
                break
            
            key = generate_key(username)
            # Menggunakan ANSI Color untuk menampilkan warna hijau di konsol
            print(f"\033[92mGenerated Key for '{username}': {key}\033[0m\n")
        except (KeyboardInterrupt, EOFError):
            print("\nExiting...")
            break
