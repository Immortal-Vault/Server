export async function getLatestClientRelease(
  repositoryOwner: string,
  repositoryName: string,
): Promise<string> {
  const url = `https://api.github.com/repos/${repositoryOwner}/${repositoryName}/releases/latest`

  try {
    const response = await fetch(url, {
      headers: {
        Authorization: `token ${process.env.GITHUB_TOKEN}`,
      },
    })

    if (!response.ok) {
      console.error(`Network response was not ok: ${response.statusText}`)
    }

    const data = await response.json()
    return data.tag_name
  } catch (error) {
    console.error('Error fetching the latest release:', error)
  }
}
