const GQL_URL = 'https://gql.twitch.tv/gql'

const WEB_CLIENT_ID = 'kimne78kx3ncx6brgo4mv6wki5h1ko'

export async function checkChannels(logins) {
  const cleaned = logins.map((l) => String(l).trim()).filter(Boolean)

  const fields = cleaned.map(
    (login, i) =>
      `c${i}: user(login: ${JSON.stringify(login)}) { profileImageURL(width: 300) stream { id title viewersCount game { displayName } } }`,
  )

  const res = await fetch(GQL_URL, {
    method: 'POST',
    headers: {
      'Client-ID': WEB_CLIENT_ID,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ query: `query { ${fields.join(' ')} }` }),
  })

  if (!res.ok) {
    const err = new Error(`HTTP ${res.status}`)
    err.status = res.status
    throw err
  }

  const payload = await res.json()

  if (payload.errors?.length) {
    throw new Error(payload.errors[0]?.message || 'GraphQL error')
  }

  const data = payload.data || {}

  return cleaned.map((login, i) => {
    const user = data[`c${i}`] ?? null
    return {
      login,
      exists: !!user,
      online: !!user?.stream,
      viewers: user?.stream?.viewersCount ?? null,
      title: user?.stream?.title ?? null,
      game: user?.stream?.game?.displayName ?? null,
      avatar: user?.profileImageURL ?? null,
    }
  })
}

export const twitch = {
  id: 'twitch',
  name: 'Twitch',
  checkChannels,
  channelUrl: (name) => `https://www.twitch.tv/${encodeURIComponent(name)}`,
}