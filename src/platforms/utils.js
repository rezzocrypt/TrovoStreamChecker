export function cleanLogins(logins) {
  return logins.map((l) => String(l).trim()).filter(Boolean)
}

export function notFoundResult(login) {
  return {
    login,
    exists: false,
    online: false,
    viewers: null,
    title: null,
    game: null,
    startedAt: null,
    avatar: null,
  }
}