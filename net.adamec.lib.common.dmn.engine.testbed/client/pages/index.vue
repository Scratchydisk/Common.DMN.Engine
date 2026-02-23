<template>
  <div class="test-lab">
    <!-- Header -->
    <div class="lab-header">
      <h1 class="lab-title">DMN Testbed</h1>
      <div class="header-right">
        <span class="dir-badge" v-if="dmnDir">{{ dmnDir }}</span>
      </div>
    </div>

    <!-- Section A: File Selector -->
    <div class="file-selector">
      <div class="selector-row">
        <label class="selector-label">DMN file:</label>
        <select v-model="selectedFileName" class="selector-dropdown" @change="onFileChange">
          <option value="">Select a file...</option>
          <option v-for="f in files" :key="f.name" :value="f.name">
            {{ f.name }}
            <template v-if="f.hasTestSuite"> (has tests)</template>
          </option>
        </select>
        <label class="btn btn-sm btn-secondary upload-btn">
          Upload .dmn
          <input type="file" accept=".dmn,.xml" @change="onFileUpload" class="hidden-input" />
        </label>
      </div>
    </div>

    <!-- Section B: DMN Viewer (collapsible) -->
    <details class="section-accordion" :open="viewerOpen" v-if="dmnXml">
      <summary class="section-summary" @click.prevent="viewerOpen = !viewerOpen">
        <h2>Decision Model</h2>
        <span class="collapse-toggle">{{ viewerOpen ? 'Collapse' : 'Expand' }}</span>
      </summary>
      <div class="section-body" v-if="viewerOpen">
        <DmnViewer
          ref="dmnViewerRef"
          :dmn-xml="dmnXml"
          :highlighted-rules="highlightedRules"
          @views-ready="onViewsReady"
        />
      </div>
    </details>

    <!-- Section C: Decision Selector -->
    <div class="decision-selector" v-if="definitionInfo && definitionInfo.decisions.length > 0">
      <div class="selector-row">
        <label class="selector-label">Decision under test:</label>
        <select v-model="selectedDecisionName" class="selector-dropdown" @change="onDecisionChange">
          <option value="">Select a decision...</option>
          <option v-for="d in definitionInfo.decisions" :key="d.name" :value="d.name">
            {{ d.name }}
          </option>
        </select>
      </div>
      <div class="decision-summary" v-if="selectedDecision">
        <span class="summary-chip">{{ selectedDecision.type }}</span>
        <span class="summary-chip" v-if="selectedDecision.hitPolicy">{{ selectedDecision.hitPolicy }} hit policy</span>
        <span class="summary-chip" v-if="selectedDecision.ruleCount">{{ selectedDecision.ruleCount }} rules</span>
        <span class="summary-chip" v-if="selectedDecision.tableInputs?.length">{{ selectedDecision.tableInputs.length }} inputs</span>
        <span class="summary-chip" v-if="selectedDecision.tableOutputs?.length">{{ selectedDecision.tableOutputs.length }} outputs</span>
        <span class="summary-chip dependency" v-if="selectedDecision.requiredDecisions?.length > 0">
          {{ selectedDecision.requiredDecisions.length }} upstream
        </span>
      </div>
    </div>

    <!-- Section D: Quick Test Form -->
    <div class="quick-test-section" v-if="selectedDecision">
      <h2 class="section-title">Quick Test</h2>
      <div class="quick-test-form">
        <div class="form-columns">
          <!-- Inputs Column -->
          <div class="form-column">
            <h3 class="column-title">Inputs</h3>
            <div class="field-list">
              <div v-for="col in formInputs" :key="col.name" class="form-field">
                <label :for="`input-${col.name}`">{{ col.label || col.name }}</label>
                <!-- Dropdown for columns with allowed values -->
                <select
                  v-if="col.allowedValues && col.allowedValues.length > 0"
                  :id="`input-${col.name}`"
                  v-model="inputValues[col.name]"
                  class="field-input"
                >
                  <option value="">-- select --</option>
                  <option v-for="val in col.allowedValues" :key="val" :value="val">{{ val }}</option>
                </select>
                <!-- Checkbox for boolean -->
                <div v-else-if="col.typeName === 'boolean'" class="checkbox-field">
                  <input
                    :id="`input-${col.name}`"
                    type="checkbox"
                    v-model="inputValues[col.name]"
                    class="field-checkbox"
                  />
                  <span class="checkbox-label">{{ inputValues[col.name] ? 'true' : 'false' }}</span>
                </div>
                <!-- Number input -->
                <input
                  v-else-if="isNumericType(col.typeName)"
                  :id="`input-${col.name}`"
                  type="number"
                  v-model.number="inputValues[col.name]"
                  class="field-input"
                  step="any"
                  :placeholder="col.typeName || 'number'"
                />
                <!-- Date input -->
                <input
                  v-else-if="col.typeName === 'date'"
                  :id="`input-${col.name}`"
                  type="date"
                  v-model="inputValues[col.name]"
                  class="field-input"
                />
                <!-- Default text input -->
                <input
                  v-else
                  :id="`input-${col.name}`"
                  type="text"
                  v-model="inputValues[col.name]"
                  class="field-input"
                  :placeholder="col.typeName || 'text'"
                />
                <span class="field-type">{{ col.typeName || col.expression }}</span>
              </div>
              <!-- Also show general input data variables not covered by table inputs -->
              <div v-for="v in extraInputs" :key="v.name" class="form-field">
                <label :for="`input-${v.name}`">{{ v.label || v.name }}</label>
                <div v-if="v.typeName === 'boolean'" class="checkbox-field">
                  <input :id="`input-${v.name}`" type="checkbox" v-model="inputValues[v.name]" class="field-checkbox" />
                  <span class="checkbox-label">{{ inputValues[v.name] ? 'true' : 'false' }}</span>
                </div>
                <input
                  v-else-if="isNumericType(v.typeName)"
                  :id="`input-${v.name}`"
                  type="number"
                  v-model.number="inputValues[v.name]"
                  class="field-input"
                  step="any"
                  :placeholder="v.typeName || 'number'"
                />
                <input
                  v-else
                  :id="`input-${v.name}`"
                  type="text"
                  v-model="inputValues[v.name]"
                  class="field-input"
                  :placeholder="v.typeName || 'text'"
                />
                <span class="field-type">{{ v.typeName }}</span>
              </div>
              <div v-if="formInputs.length === 0 && extraInputs.length === 0" class="no-fields">
                No inputs defined for this decision.
              </div>
            </div>
          </div>

          <!-- Expected Outputs Column -->
          <div class="form-column">
            <h3 class="column-title">Expected Outputs</h3>
            <div class="field-list">
              <div v-for="col in formOutputs" :key="col.name" class="form-field">
                <label :for="`output-${col.name}`">{{ col.label || col.name }}</label>
                <select
                  v-if="col.allowedValues && col.allowedValues.length > 0"
                  :id="`output-${col.name}`"
                  v-model="expectedOutputs[col.name]"
                  class="field-input"
                >
                  <option value="">-- any --</option>
                  <option v-for="val in col.allowedValues" :key="val" :value="val">{{ val }}</option>
                </select>
                <div v-else-if="col.typeName === 'boolean'" class="checkbox-field">
                  <input
                    :id="`output-${col.name}`"
                    type="checkbox"
                    v-model="expectedOutputs[col.name]"
                    class="field-checkbox"
                  />
                  <span class="checkbox-label">{{ expectedOutputs[col.name] ? 'true' : 'false' }}</span>
                </div>
                <input
                  v-else-if="isNumericType(col.typeName)"
                  :id="`output-${col.name}`"
                  type="number"
                  v-model.number="expectedOutputs[col.name]"
                  class="field-input"
                  step="any"
                  placeholder="expected value (optional)"
                />
                <input
                  v-else
                  :id="`output-${col.name}`"
                  type="text"
                  v-model="expectedOutputs[col.name]"
                  class="field-input"
                  placeholder="expected value (optional)"
                />
                <span class="field-type">{{ col.typeName }}</span>
              </div>
              <div v-if="formOutputs.length === 0" class="no-fields">
                No outputs defined for this decision.
              </div>
            </div>
            <p class="output-hint">Leave blank to see actual result only.</p>
          </div>
        </div>

        <!-- Actions -->
        <div class="form-actions">
          <button class="btn btn-evaluate" @click="executeDecision" :disabled="executing">
            {{ executing ? 'Executing...' : 'Execute' }}
          </button>
          <button class="btn btn-save" @click="openSaveDialog" :disabled="!hasAnyInput">
            Save as Test Case
          </button>
          <button class="btn btn-clear" @click="clearForm">
            Clear
          </button>
        </div>
      </div>

      <!-- Result Panel -->
      <div class="result-panel" v-if="lastResult">
        <div v-if="lastResult.error" class="result-banner error">
          <strong>Error:</strong> {{ lastResult.error }}
        </div>
        <div v-else-if="lastResult.hasResult" class="result-section">
          <div class="result-header">
            <h3>Result</h3>
            <span class="timing">{{ lastResult.executionTimeMs }}ms</span>
          </div>

          <div v-for="(sr, idx) in lastResult.results" :key="idx" class="result-row">
            <div class="result-outputs">
              <div v-for="(out, outName) in sr.outputs" :key="outName" class="output-item">
                <span class="output-name">{{ outName }}</span>
                <span class="output-value" :class="getOutputClass(outName, out.value)">{{ formatValue(out.value) }}</span>
                <span class="output-type">{{ out.typeName }}</span>
                <span v-if="expectedOutputs[outName] !== '' && expectedOutputs[outName] != null" class="match-indicator" :class="valuesMatch(expectedOutputs[outName], out.value) ? 'match' : 'mismatch'">
                  {{ valuesMatch(expectedOutputs[outName], out.value) ? 'PASS' : 'FAIL' }}
                </span>
              </div>
            </div>
            <div v-if="sr.hitRules?.length" class="hit-rules">
              <span class="hit-label">Hit rules:</span>
              <span v-for="hr in sr.hitRules" :key="hr.index" class="hit-rule">
                #{{ hr.index + 1 }}<template v-if="hr.name"> ({{ hr.name }})</template>
              </span>
            </div>
          </div>

          <!-- Execution trace -->
          <details v-if="lastResult.steps?.length" class="trace-accordion">
            <summary class="trace-summary">Execution trace ({{ lastResult.steps.length }} steps)</summary>
            <div class="trace-body">
              <div v-for="(step, idx) in lastResult.steps" :key="idx" class="trace-step">
                <div class="step-header">
                  Step {{ idx + 1 }}: {{ step.decisionName }} [{{ step.decisionType }}]
                </div>
                <div v-if="step.hitRules?.length" class="step-hits">
                  <span v-for="hr in step.hitRules" :key="hr.index" class="hit-rule">
                    Rule #{{ hr.index + 1 }}<template v-if="hr.name"> ({{ hr.name }})</template>
                  </span>
                </div>
                <div v-if="Object.keys(step.variableChanges || {}).length" class="step-changes">
                  <div v-for="(val, name) in step.variableChanges" :key="name" class="change-item">
                    {{ name }} = {{ formatValue(val) }}
                  </div>
                </div>
              </div>
            </div>
          </details>
        </div>
        <div v-else class="result-banner info">
          No result returned from engine.
        </div>
      </div>
    </div>

    <!-- Save Test Case Dialog -->
    <div v-if="showSaveDialog" class="dialog-overlay" @click.self="showSaveDialog = false">
      <div class="dialog">
        <h3>Save Test Case</h3>
        <div class="dialog-field">
          <label for="test-case-name">Name</label>
          <input
            id="test-case-name"
            v-model="newTestCaseName"
            type="text"
            class="field-input"
            placeholder="e.g. Happy path - residential customer"
            @keyup.enter="saveTestCase"
            ref="testCaseNameInput"
          />
        </div>
        <div class="dialog-actions">
          <button class="btn btn-primary" @click="saveTestCase" :disabled="!newTestCaseName.trim() || savingTestCase">
            {{ savingTestCase ? 'Saving...' : 'Save' }}
          </button>
          <button class="btn btn-secondary" @click="showSaveDialog = false">Cancel</button>
        </div>
      </div>
    </div>

    <!-- Section E: Test Suite Table -->
    <div class="test-suite-section" v-if="selectedFileName">
      <div class="suite-header">
        <h2 class="section-title">Test Suite ({{ testCases.length }} cases)</h2>
        <div class="suite-actions">
          <button class="btn btn-sm btn-evaluate" @click="runAllTests" :disabled="testCases.length === 0 || runningAll">
            {{ runningAll ? 'Running...' : 'Run All' }}
          </button>
          <label class="btn btn-sm btn-secondary import-btn">
            Import
            <input type="file" accept=".json" @change="importTestCases" class="hidden-input" />
          </label>
          <button class="btn btn-sm btn-secondary" @click="exportTestCases" :disabled="testCases.length === 0">
            Export
          </button>
        </div>
      </div>

      <div v-if="testCases.length === 0" class="empty-suite">
        <p>No test cases yet. Use the Quick Test form above to create your first test case.</p>
      </div>

      <div v-else class="suite-table-wrapper">
        <table class="suite-table">
          <thead>
            <tr>
              <th class="col-status">Status</th>
              <th class="col-name">Name</th>
              <th class="col-decision">Decision</th>
              <th class="col-inputs">Inputs</th>
              <th class="col-result">Last Run</th>
              <th class="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="tc in testCases"
              :key="tc.id"
              class="suite-row"
              :class="{ active: activeTestCaseId === tc.id }"
              @click="loadTestCase(tc)"
            >
              <td class="col-status">
                <span class="status-icon" :class="getStatusClass(tc)">{{ getStatusIcon(tc) }}</span>
              </td>
              <td class="col-name">{{ tc.name }}</td>
              <td class="col-decision">{{ tc.decisionName }}</td>
              <td class="col-inputs">
                <span class="inputs-summary">{{ formatInputsSummary(tc.inputs) }}</span>
              </td>
              <td class="col-result">
                <span v-if="tc.lastRun" :class="tc.lastRun.status === 'pass' ? 'result-pass' : tc.lastRun.status === 'error' ? 'result-error' : 'result-fail'">
                  {{ tc.lastRun.status === 'pass' ? 'Pass' : tc.lastRun.status === 'error' ? 'Error' : 'Fail' }}
                </span>
                <span v-else class="result-none">--</span>
              </td>
              <td class="col-actions" @click.stop>
                <button class="action-btn" @click="runSingleTest(tc)" title="Run">
                  <svg class="action-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </button>
                <button class="action-btn" @click="duplicateTestCase(tc)" title="Duplicate">
                  <svg class="action-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                  </svg>
                </button>
                <button class="action-btn danger" @click="deleteTestCase(tc)" title="Delete">
                  <svg class="action-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p class="table-hint">Click a row to load it into the Quick Test form.</p>
      </div>
    </div>

    <!-- Loading overlay -->
    <div v-if="loading" class="loading-page">
      <div class="loading-spinner"></div>
      <span>Loading...</span>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useApi } from '~/composables/useApi'

const api = useApi()

// Page state
const loading = ref(false)
const files = ref([])
const selectedFileName = ref('')
const dmnXml = ref(null)
const dmnDir = ref('')
const definitionInfo = ref(null)
const viewerOpen = ref(true)

// Decision state
const selectedDecisionName = ref('')
const dmnViewerRef = ref(null)

// Quick test form
const inputValues = ref({})
const expectedOutputs = ref({})
const executing = ref(false)
const lastResult = ref(null)
const highlightedRules = ref([])

// Save dialog
const showSaveDialog = ref(false)
const newTestCaseName = ref('')
const savingTestCase = ref(false)
const testCaseNameInput = ref(null)

// Test suite
const testCases = ref([])
const activeTestCaseId = ref(null)
const runningAll = ref(false)

// Computed
const selectedDecision = computed(() => {
  if (!selectedDecisionName.value || !definitionInfo.value) return null
  return definitionInfo.value.decisions.find(d => d.name === selectedDecisionName.value) || null
})

const formInputs = computed(() => {
  if (!selectedDecision.value) return []
  if (selectedDecision.value.type === 'table') {
    return selectedDecision.value.tableInputs || []
  }
  return []
})

const formOutputs = computed(() => {
  if (!selectedDecision.value) return []
  if (selectedDecision.value.type === 'table') {
    return selectedDecision.value.tableOutputs || []
  }
  return []
})

// Input data variables required by this decision but not covered by table inputs
const extraInputs = computed(() => {
  if (!selectedDecision.value || !definitionInfo.value) return []
  const tableInputNames = new Set((selectedDecision.value.tableInputs || []).map(i => i.name))
  return (selectedDecision.value.requiredInputs || [])
    .filter(name => !tableInputNames.has(name))
    .map(name => {
      const v = definitionInfo.value.inputData.find(i => i.name === name)
      return v || { name, typeName: null }
    })
})

const hasAnyInput = computed(() => {
  return Object.values(inputValues.value).some(v => v !== '' && v !== null && v !== undefined && v !== false)
})

// Helper functions
function isNumericType(typeName) {
  return ['number', 'integer', 'long', 'double', 'decimal'].includes(typeName)
}

function formatValue(value) {
  if (value === null || value === undefined) return 'null'
  if (typeof value === 'string') return `"${value}"`
  if (typeof value === 'boolean') return value ? 'true' : 'false'
  return String(value)
}

function valuesMatch(expected, actual) {
  if (expected === '' || expected === null || expected === undefined) return true
  const expStr = String(expected)
  const actStr = String(actual)
  return expStr.toLowerCase() === actStr.toLowerCase()
}

function getOutputClass(name, value) {
  if (expectedOutputs.value[name] === '' || expectedOutputs.value[name] == null) return ''
  return valuesMatch(expectedOutputs.value[name], value) ? 'output-pass' : 'output-fail'
}

// File operations
async function loadFileList() {
  try {
    files.value = await api.listFiles()
  } catch (err) {
    console.error('Failed to load file list:', err)
  }
}

async function onFileChange() {
  if (!selectedFileName.value) {
    dmnXml.value = null
    definitionInfo.value = null
    testCases.value = []
    return
  }

  loading.value = true
  try {
    const [xml, info, suite] = await Promise.all([
      api.getXml(selectedFileName.value),
      api.getInfo(selectedFileName.value),
      api.loadTests(selectedFileName.value)
    ])

    dmnXml.value = xml
    definitionInfo.value = info
    testCases.value = suite.testCases || []

    // Auto-select first decision with rules
    const firstTable = info.decisions.find(d => d.ruleCount > 0)
    if (firstTable) {
      selectedDecisionName.value = firstTable.name
    } else if (info.decisions.length > 0) {
      selectedDecisionName.value = info.decisions[0].name
    } else {
      selectedDecisionName.value = ''
    }

    initFormFields()
    lastResult.value = null
    highlightedRules.value = []
  } catch (err) {
    console.error('Failed to load DMN file:', err)
  } finally {
    loading.value = false
  }
}

async function onFileUpload(event) {
  const file = event.target.files?.[0]
  if (!file) return

  try {
    const xml = await file.text()
    dmnXml.value = xml
    selectedFileName.value = ''
    definitionInfo.value = null
    testCases.value = []
    lastResult.value = null
    highlightedRules.value = []
  } catch (err) {
    console.error('Failed to read uploaded file:', err)
  }
  event.target.value = ''
}

// Decision operations
function onDecisionChange() {
  initFormFields()
  lastResult.value = null
  highlightedRules.value = []
  activeTestCaseId.value = null

  // Sync viewer to selected decision
  if (dmnViewerRef.value && selectedDecisionName.value) {
    // Try to find the decision's ID from the XML model for the viewer
    const decision = definitionInfo.value?.decisions.find(d => d.name === selectedDecisionName.value)
    if (decision) {
      dmnViewerRef.value.switchToDecision(decision.name)
    }
  }
}

function onViewsReady() {
  if (selectedDecisionName.value && dmnViewerRef.value) {
    dmnViewerRef.value.switchToDecision(selectedDecisionName.value)
  }
}

function initFormFields() {
  const inputs = {}
  const outputs = {}

  if (selectedDecision.value) {
    // Table inputs
    for (const col of (selectedDecision.value.tableInputs || [])) {
      inputs[col.name] = col.typeName === 'boolean' ? false : ''
    }
    // Extra required inputs
    for (const name of (selectedDecision.value.requiredInputs || [])) {
      if (!(name in inputs)) {
        const v = definitionInfo.value?.inputData.find(i => i.name === name)
        inputs[name] = v?.typeName === 'boolean' ? false : ''
      }
    }
    // Table outputs
    for (const col of (selectedDecision.value.tableOutputs || [])) {
      outputs[col.name] = ''
    }
  }

  inputValues.value = inputs
  expectedOutputs.value = outputs
}

function clearForm() {
  initFormFields()
  lastResult.value = null
  highlightedRules.value = []
  activeTestCaseId.value = null
}

// Execution
async function executeDecision() {
  if (!selectedDecisionName.value || !selectedFileName.value) return

  executing.value = true
  lastResult.value = null
  highlightedRules.value = []

  try {
    // Build clean inputs (skip empty strings)
    const cleanInputs = {}
    for (const [k, v] of Object.entries(inputValues.value)) {
      if (v !== '' && v !== null && v !== undefined) {
        cleanInputs[k] = v
      }
    }

    const result = await api.execute(selectedFileName.value, selectedDecisionName.value, cleanInputs)
    lastResult.value = result

    // Extract hit rule indices for highlighting
    if (result.hasResult && result.results?.length > 0) {
      highlightedRules.value = result.results.flatMap(r => (r.hitRules || []).map(h => h.index))
    }
  } catch (err) {
    lastResult.value = { error: err.message || 'Execution failed' }
  } finally {
    executing.value = false
  }
}

// Save dialog
function openSaveDialog() {
  newTestCaseName.value = ''
  showSaveDialog.value = true
  nextTick(() => {
    testCaseNameInput.value?.focus()
  })
}

async function saveTestCase() {
  if (!newTestCaseName.value.trim() || !selectedDecision.value || !selectedFileName.value) return

  savingTestCase.value = true

  try {
    const tc = {
      id: crypto.randomUUID(),
      name: newTestCaseName.value.trim(),
      decisionName: selectedDecisionName.value,
      inputs: { ...inputValues.value },
      expectedOutputs: { ...expectedOutputs.value },
      lastRun: null
    }

    testCases.value.push(tc)
    await saveTestSuiteToServer()
    showSaveDialog.value = false
  } catch (err) {
    console.error('Failed to save test case:', err)
    alert(`Failed to save test case: ${err.message}`)
  } finally {
    savingTestCase.value = false
  }
}

async function saveTestSuiteToServer() {
  if (!selectedFileName.value) return

  const suite = {
    version: 1,
    dmnFile: selectedFileName.value.split('/').pop(),
    testCases: testCases.value
  }

  await api.saveTests(selectedFileName.value, suite)
}

// Test case operations
function loadTestCase(tc) {
  // Switch decision if different
  if (tc.decisionName !== selectedDecisionName.value) {
    selectedDecisionName.value = tc.decisionName
    initFormFields()
  }

  // Populate inputs
  if (tc.inputs) {
    for (const [key, val] of Object.entries(tc.inputs)) {
      if (key in inputValues.value) {
        inputValues.value[key] = val
      }
    }
  }

  // Populate expected outputs
  if (tc.expectedOutputs) {
    for (const [key, val] of Object.entries(tc.expectedOutputs)) {
      if (key in expectedOutputs.value) {
        expectedOutputs.value[key] = val
      }
    }
  }

  activeTestCaseId.value = tc.id
  lastResult.value = null
  highlightedRules.value = []

  // Show last run result if available
  if (tc.lastRun?.actualOutputs) {
    highlightedRules.value = tc.lastRun.hitRules || []
  }
}

async function duplicateTestCase(tc) {
  const dup = {
    id: crypto.randomUUID(),
    name: `${tc.name} (copy)`,
    decisionName: tc.decisionName,
    inputs: { ...tc.inputs },
    expectedOutputs: { ...tc.expectedOutputs },
    lastRun: null
  }

  testCases.value.push(dup)
  await saveTestSuiteToServer()
}

async function deleteTestCase(tc) {
  if (!confirm(`Delete test case "${tc.name}"?`)) return

  testCases.value = testCases.value.filter(t => t.id !== tc.id)
  if (activeTestCaseId.value === tc.id) {
    activeTestCaseId.value = null
  }
  await saveTestSuiteToServer()
}

async function runSingleTest(tc) {
  try {
    const result = await api.runTests(selectedFileName.value, [tc.id])
    if (result.results?.length > 0) {
      const r = result.results[0]
      tc.lastRun = {
        status: r.status,
        actualOutputs: r.actualOutputs,
        hitRules: r.hitRules,
        executionTimeMs: r.executionTimeMs,
        error: r.error,
        ranAt: new Date().toISOString()
      }
    }
    // Refresh suite from server to stay in sync
    const suite = await api.loadTests(selectedFileName.value)
    testCases.value = suite.testCases || []
  } catch (err) {
    console.error('Failed to run test:', err)
  }
}

async function runAllTests() {
  runningAll.value = true
  try {
    const result = await api.runTests(selectedFileName.value)
    // Refresh suite from server
    const suite = await api.loadTests(selectedFileName.value)
    testCases.value = suite.testCases || []
  } catch (err) {
    console.error('Failed to run tests:', err)
  } finally {
    runningAll.value = false
  }
}

// Import/Export
function exportTestCases() {
  const exportData = {
    version: 1,
    dmnFile: selectedFileName.value.split('/').pop(),
    testCases: testCases.value
  }

  const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${selectedFileName.value.replace(/[/\\]/g, '-').replace('.dmn', '')}.tests.json`
  link.click()
  URL.revokeObjectURL(url)
}

async function importTestCases(event) {
  const file = event.target.files?.[0]
  if (!file) return

  try {
    const text = await file.text()
    const imported = JSON.parse(text)

    let cases = []
    if (Array.isArray(imported)) {
      cases = imported
    } else if (imported.testCases && Array.isArray(imported.testCases)) {
      cases = imported.testCases
    } else {
      alert('Invalid file: expected a JSON object with testCases array.')
      return
    }

    let added = 0
    for (const tc of cases) {
      if (!tc.decisionName || !tc.name) continue

      testCases.value.push({
        id: tc.id || crypto.randomUUID(),
        name: tc.name,
        decisionName: tc.decisionName,
        inputs: tc.inputs || {},
        expectedOutputs: tc.expectedOutputs || {},
        lastRun: null
      })
      added++
    }

    await saveTestSuiteToServer()
    alert(`Imported ${added} test case${added !== 1 ? 's' : ''}.`)
  } catch (err) {
    console.error('Import failed:', err)
    alert(`Import failed: ${err.message}`)
  }

  event.target.value = ''
}

// Table helpers
function getStatusIcon(tc) {
  if (!tc.lastRun) return '\u25CB'  // empty circle
  if (tc.lastRun.status === 'pass') return '\u2713'  // check mark
  if (tc.lastRun.status === 'error') return '!'
  return '\u2717'  // cross mark
}

function getStatusClass(tc) {
  if (!tc.lastRun) return 'status-pending'
  if (tc.lastRun.status === 'pass') return 'status-pass'
  if (tc.lastRun.status === 'error') return 'status-error'
  return 'status-fail'
}

function formatInputsSummary(inputs) {
  if (!inputs) return ''
  const values = Object.entries(inputs)
    .filter(([, v]) => v !== '' && v !== null && v !== undefined && v !== false)
    .map(([k, v]) => `${k}=${v}`)
  if (values.length === 0) return '(empty)'
  const display = values.slice(0, 3).join(', ')
  return values.length > 3 ? `${display}, ...` : display
}

// Init
onMounted(async () => {
  await loadFileList()
})
</script>

<style scoped>
.test-lab {
  @apply max-w-6xl mx-auto pb-12 px-4;
}

/* Header */
.lab-header {
  @apply flex items-center justify-between mb-6 pb-4 border-b border-gray-200 pt-6;
}

.lab-title {
  @apply text-lg font-bold text-gray-800;
}

.header-right {
  @apply text-right;
}

.dir-badge {
  @apply inline-block px-2 py-1 text-xs font-mono rounded bg-gray-100 text-gray-500;
}

/* File Selector */
.file-selector {
  @apply mb-6 p-4 bg-white border border-gray-200 rounded-lg;
}

/* Section Accordion */
.section-accordion {
  @apply mb-6 border border-gray-200 rounded-lg overflow-hidden bg-white;
}

.section-summary {
  @apply p-3 bg-emerald-50 border-b border-emerald-200 cursor-pointer flex items-center justify-between select-none transition-colors duration-200 hover:bg-emerald-100 list-none;
}

.section-summary::-webkit-details-marker {
  display: none;
}

.section-summary h2 {
  @apply m-0 text-sm font-semibold text-emerald-800;
}

.collapse-toggle {
  @apply text-xs text-emerald-600 font-medium;
}

.section-body {
  @apply p-4;
}

/* Decision Selector */
.decision-selector {
  @apply mb-6 p-4 bg-white border border-gray-200 rounded-lg;
}

.selector-row {
  @apply flex items-center gap-3 mb-2 flex-wrap;
}

.selector-label {
  @apply text-sm font-medium text-gray-700 whitespace-nowrap;
}

.selector-dropdown {
  @apply flex-1 max-w-md px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500;
}

.decision-summary {
  @apply flex flex-wrap gap-2 mt-2;
}

.summary-chip {
  @apply inline-block px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-600;
}

.summary-chip.dependency {
  @apply bg-amber-50 text-amber-700;
}

/* Quick Test Section */
.quick-test-section {
  @apply mb-6;
}

.section-title {
  @apply text-base font-semibold text-gray-800 mb-3;
}

.quick-test-form {
  @apply p-4 bg-white border border-gray-200 rounded-lg;
}

.form-columns {
  @apply grid grid-cols-1 md:grid-cols-2 gap-6;
}

.form-column {
  @apply space-y-1;
}

.column-title {
  @apply text-sm font-semibold text-gray-700 mb-3 pb-2 border-b border-gray-100;
}

.field-list {
  @apply space-y-3;
}

.form-field {
  @apply space-y-1;
}

.form-field label {
  @apply block text-xs font-medium text-gray-600;
}

.field-input {
  @apply w-full px-2.5 py-1.5 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-1 focus:ring-emerald-500 focus:border-emerald-500;
}

.checkbox-field {
  @apply flex items-center gap-2;
}

.field-checkbox {
  @apply w-4 h-4 text-emerald-600 border-gray-300 rounded focus:ring-emerald-500;
}

.checkbox-label {
  @apply text-sm text-gray-600;
}

.field-type {
  @apply text-xs text-gray-400 font-mono;
}

.no-fields {
  @apply text-sm text-gray-400 italic py-4;
}

.output-hint {
  @apply text-xs text-gray-400 italic mt-3;
}

/* Form Actions */
.form-actions {
  @apply flex gap-2 mt-6 pt-4 border-t border-gray-100;
}

.btn {
  @apply px-4 py-2 text-sm font-medium rounded-md transition-colors cursor-pointer;
}

.btn-evaluate {
  @apply bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed;
}

.btn-save {
  @apply bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed;
}

.btn-clear {
  @apply bg-gray-100 text-gray-700 hover:bg-gray-200;
}

.btn-primary {
  @apply bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed;
}

.btn-secondary {
  @apply bg-gray-100 text-gray-700 hover:bg-gray-200;
}

.btn-sm {
  @apply px-3 py-1.5 text-xs;
}

/* Result Panel */
.result-panel {
  @apply mt-4;
}

.result-banner {
  @apply p-3 rounded-lg text-sm;
}

.result-banner.error {
  @apply bg-red-50 border border-red-200 text-red-700;
}

.result-banner.info {
  @apply bg-amber-50 border border-amber-200 text-amber-800;
}

.result-section {
  @apply bg-white border border-gray-200 rounded-lg p-4;
}

.result-header {
  @apply flex items-center justify-between mb-3 pb-2 border-b border-gray-100;
}

.result-header h3 {
  @apply text-sm font-semibold text-gray-700;
}

.timing {
  @apply text-xs text-gray-400 font-mono;
}

.result-row {
  @apply mb-3;
}

.result-outputs {
  @apply space-y-1;
}

.output-item {
  @apply flex items-center gap-2 text-sm;
}

.output-name {
  @apply font-medium text-gray-700 min-w-[120px];
}

.output-value {
  @apply font-mono text-gray-900;
}

.output-value.output-pass {
  @apply text-green-700;
}

.output-value.output-fail {
  @apply text-red-700;
}

.output-type {
  @apply text-xs text-gray-400;
}

.match-indicator {
  @apply text-xs font-bold px-1.5 py-0.5 rounded;
}

.match-indicator.match {
  @apply bg-green-100 text-green-700;
}

.match-indicator.mismatch {
  @apply bg-red-100 text-red-700;
}

.hit-rules {
  @apply mt-2 text-xs text-gray-500;
}

.hit-label {
  @apply font-medium;
}

.hit-rule {
  @apply inline-block ml-1 px-1.5 py-0.5 bg-emerald-50 text-emerald-700 rounded;
}

/* Trace */
.trace-accordion {
  @apply mt-3 border border-gray-100 rounded;
}

.trace-summary {
  @apply px-3 py-2 text-xs font-medium text-gray-500 cursor-pointer hover:bg-gray-50;
}

.trace-body {
  @apply px-3 pb-3;
}

.trace-step {
  @apply mb-2 text-xs;
}

.step-header {
  @apply font-medium text-gray-700;
}

.step-hits {
  @apply ml-4 text-gray-500;
}

.step-changes {
  @apply ml-4 text-gray-600 font-mono;
}

.change-item {
  @apply py-0.5;
}

/* Save Dialog */
.dialog-overlay {
  @apply fixed inset-0 z-50 flex items-center justify-center bg-black/30;
}

.dialog {
  @apply bg-white rounded-xl shadow-xl p-6 w-full max-w-md;
}

.dialog h3 {
  @apply text-base font-semibold text-gray-800 mb-4;
}

.dialog-field {
  @apply mb-4;
}

.dialog-field label {
  @apply block text-sm font-medium text-gray-700 mb-1;
}

.dialog-actions {
  @apply flex gap-2 justify-end;
}

/* Test Suite Section */
.test-suite-section {
  @apply mb-6;
}

.suite-header {
  @apply flex items-center justify-between mb-3;
}

.suite-actions {
  @apply flex gap-2;
}

.upload-btn, .import-btn {
  @apply cursor-pointer;
}

.hidden-input {
  @apply hidden;
}

.empty-suite {
  @apply text-center py-8 text-gray-500 bg-white border border-gray-200 rounded-lg;
}

.suite-table-wrapper {
  @apply bg-white border border-gray-200 rounded-lg overflow-hidden;
}

.suite-table {
  @apply w-full text-sm;
}

.suite-table thead {
  @apply bg-gray-50 border-b border-gray-200;
}

.suite-table th {
  @apply px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider;
}

.suite-table td {
  @apply px-3 py-2.5;
}

.suite-row {
  @apply border-b border-gray-100 cursor-pointer hover:bg-blue-50 transition-colors;
}

.suite-row.active {
  @apply bg-blue-50;
}

.col-status {
  @apply w-12 text-center;
}

.col-name {
  @apply font-medium text-gray-800;
}

.col-decision {
  @apply text-gray-600;
}

.col-inputs {
  @apply max-w-xs;
}

.inputs-summary {
  @apply text-xs text-gray-500 font-mono;
}

.col-result {
  @apply w-20;
}

.col-actions {
  @apply w-28 text-right;
}

.result-pass {
  @apply text-xs font-medium text-green-700 bg-green-50 px-2 py-0.5 rounded;
}

.result-fail {
  @apply text-xs font-medium text-red-700 bg-red-50 px-2 py-0.5 rounded;
}

.result-error {
  @apply text-xs font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded;
}

.result-none {
  @apply text-xs text-gray-400;
}

.action-btn {
  @apply inline-flex items-center justify-center w-7 h-7 rounded hover:bg-gray-100 transition-colors;
}

.action-btn.danger {
  @apply hover:bg-red-50;
}

.action-icon {
  @apply w-4 h-4 text-gray-500;
}

.action-btn.danger .action-icon {
  @apply text-red-500;
}

.status-icon {
  @apply text-sm font-bold;
}

.status-pending {
  @apply text-gray-400;
}

.status-pass {
  @apply text-green-600;
}

.status-fail {
  @apply text-red-600;
}

.status-error {
  @apply text-amber-600;
}

.table-hint {
  @apply text-xs text-gray-400 text-center py-2;
}

/* Loading */
.loading-page {
  @apply fixed inset-0 flex items-center justify-center gap-3 bg-white/80 text-gray-500 z-40;
}

.loading-spinner {
  @apply w-5 h-5 border-2 border-gray-300 border-t-emerald-600 rounded-full animate-spin;
}

@media (max-width: 768px) {
  .lab-header {
    @apply flex-col gap-2 text-center;
  }

  .form-columns {
    @apply grid-cols-1;
  }

  .suite-actions {
    @apply flex-wrap;
  }
}
</style>
