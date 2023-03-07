<template>
    <div class="input">
        <label for="audioInputDeviceSelect">Audio Input Device:</label>
        <select v-model="selectedAudioInputDeviceId" id="audioInputDeviceSelect">
            <option v-for="device in audioInputDevices" :key="device.deviceId" :value="device.deviceId">
                {{ device.label }}
            </option>
        </select>
    </div>
    <div class="input">
        <label for="channel">Channel</label>
        <Combobox v-model="channel" :options="channels" class="form-control" id="channel" />
    </div>
    <div>
        <button @click="enabled = !enabled">
            {{ enabled ? 'Stop' : 'Start' }}
        </button>
    </div>
</template>

<script setup>
import { useIngestion } from '../hooks/useIngestion.js'
import Combobox from './UI/Combobox.vue';
import { useChannels } from '../hooks/useChannels';

const channels = useChannels();

const { channel, audioInputDevices, enabled, selectedAudioInputDeviceId } = useIngestion();

</script>