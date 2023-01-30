import { ref } from 'vue'

export function useChannels() {

    const channels = ref([
        { value: "1", text: "BBC" },
        { value: "2", text: "MSC" },
    ])

    return channels
}