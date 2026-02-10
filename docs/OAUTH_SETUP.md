# GitHub OAuth Setup for TurboGit

To use GitHub integration in TurboGit, you need to register a new OAuth application on GitHub.

## Registration Steps

1.  Go to **Settings** > **Developer settings** > **OAuth Apps** on GitHub.
2.  Click **New OAuth App**.
3.  Fill in the form as follows:

    -   **Application name**: `TurboGit` (or a name of your choice)
    -   **Homepage URL**: `http://localhost:8989`
    -   **Application description**: `A fast and efficient Git client.` (Optional)
    -   **Authorization callback URL**: `http://localhost:8989/callback/`
        *Note: The trailing slash is important for the local callback listener.*

4.  Click **Register application**.

## Configuration

After registering the application:

1.  Copy the **Client ID**.
2.  Generate a new **Client Secret** and copy it.
3.  Set the following environment variables on your system:

    -   `TURBOGIT_GITHUB_CLIENT_ID`: Your Client ID
    -   `TURBOGIT_GITHUB_CLIENT_SECRET`: Your Client Secret

You can set these in your shell profile (e.g., `.bashrc`, `.zshrc`) or pass them directly when running the application.

## Troubleshooting

-   Ensure that no other application is using port `8989`.
-   Verify that the callback URL exactly matches `http://localhost:8989/callback/` (including the trailing slash).
