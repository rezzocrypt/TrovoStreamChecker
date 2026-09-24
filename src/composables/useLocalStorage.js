import { ref, watch } from 'vue'

export function useLocalStorage(key, getDefault) {
  const value = ref(load())

  function load() {
    try {
      const raw = localStorage.getItem(key)
      if (raw !== null) {
        return JSON.parse(raw)
      }
    } catch {
      // ignore corrupted storage
    }
    return getDefault()
  }

  watch(
    value,
    (v) => {
      localStorage.setItem(key, JSON.stringify(v))
    },
    { deep: true },
  )

  return value
}