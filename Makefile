.PHONY: help build test pack clean

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-10s\033[0m %s\n", $$1, $$2}'

build: ## Build all projects
	dotnet build

test: ## Run all tests (18 tests)
	dotnet test

pack: ## Pack NuGet packages to ./artifacts
	dotnet pack src/Promises/Promises.csproj --configuration Release --output ./artifacts
	dotnet pack src/Promises.InMemory/Promises.InMemory.csproj --configuration Release --output ./artifacts
	dotnet pack src/Promises.FileSystem/Promises.FileSystem.csproj --configuration Release --output ./artifacts

clean: ## Clean build artifacts
	dotnet clean
	rm -rf ./artifacts
