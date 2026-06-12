{
  description = "WinHome development environment";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs {
          inherit system;
          config = {
            # .NET packages sometimes require accepting unfree licenses depending on the build/channel
            allowUnfree = true;
          };
        };

        # Updated: Matches 'nix-shell -p dotnetCorePackages.sdk_10_0-bin'
        # Alternatively, use 'pkgs.dotnet-sdk_10' if that's the preferred alias in your pin
        dotnetSdk = pkgs.dotnetCorePackages.sdk_10_0-bin;
        uvPkg = pkgs.uv;

        mkApp = pkg: {
          type = "app";
          program = "${pkg}/bin/${pkg.name}";
        };
      in
      {
        devShells.default = pkgs.mkShell {
          name = "winhome-dev";
          packages = with pkgs; [
            dotnetSdk
            bun
            python3
            uvPkg
            git
          ];

          shellHook = ''
            export DOTNET_SYSTEM_CONSOLE_USE_ANSI_COLOR_PALETTES=true
          '';
        };

        apps = {
          build = mkApp (pkgs.writeShellApplication {
            name = "winhome-build";
            text = ''
              export PATH="${dotnetSdk}/bin:$PATH"
              exec dotnet build "$@"
            '';
          });

          test = mkApp (pkgs.writeShellApplication {
            name = "winhome-test";
            text = ''
              export PATH="${dotnetSdk}/bin:$PATH"
              exec dotnet test "$@"
            '';
          });

          run = mkApp (pkgs.writeShellApplication {
            name = "winhome-run";
            text = ''
              export PATH="${dotnetSdk}/bin:$PATH"
              exec dotnet run --project src/WinHome.csproj -- "$@"
            '';
          });

          uv-sync = mkApp (pkgs.writeShellApplication {
            name = "winhome-uv-sync";
            text = ''
              export PATH="${uvPkg}/bin:$PATH"
              if [ -f "requirements.txt" ]; then
                uv pip sync requirements.txt
              elif [ -f "pyproject.toml" ]; then
                uv pip sync
              else
                echo "No requirements.txt or pyproject.toml found"
              fi
            '';
          });
        };

        defaultPackage = pkgs.writeShellApplication {
          name = "winhome-build";
          text = ''
            export PATH="${dotnetSdk}/bin:$PATH"
            exec dotnet build "$@"
          '';
        };
      }
    );
}
