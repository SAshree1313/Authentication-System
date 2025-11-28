import React, { useContext, useEffect, useState } from "react";
import { AuthContext } from "../auth/AuthContext";
import {
  getDevices,
  updateDeviceName,
  deleteDevice,
  startRegisterExistingDevice,
  finishRegister,
  deleteAccount,
} from "../services/MultiDeviceService.ts";
import { FaDesktop, FaSave, FaEdit, FaTrash, FaPlus, FaSignOutAlt } from "react-icons/fa";
import {
  prepareCredentialCreateOptions,
  attestationToJSON
} from '../utils/WebAuthn.ts';
import "./WelcomePage.css";

export default function WelcomePage() {
  const { user, logout } = useContext(AuthContext);
  const [devices, setDevices] = useState([]);
  const [editingId, setEditingId] = useState(null);
  const [newDeviceName, setNewDeviceName] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);
  const [addDeviceName, setAddDeviceName] = useState("");

  useEffect(() => {
    fetchDevices();
  }, []);

  const fetchDevices = async () => {
    try {
      const res = await getDevices();
      setDevices(res.devices);
    } catch (err) {
      console.error(err);
      setMessage("Failed to fetch devices");
    }
  };

  const handleSaveDeviceName = async (credentialId) => {
    if (!newDeviceName.trim()) return;
    try {
      const updated = await updateDeviceName(credentialId, newDeviceName.trim());
      setDevices(prev => prev.map(d => (d.credentialId === credentialId ? updated : d)));
      setEditingId(null);
      setNewDeviceName("");
    } catch (err) {
      console.error(err);
      setMessage("Error updating device name");
    }
  };

  const handleDeleteDevice = async (credentialId) => {
    if (!window.confirm("Are you sure you want to delete this device?")) return;
    try {
      await deleteDevice(credentialId);
      setDevices(prev => prev.filter(d => d.credentialId !== credentialId));
    } catch (err) {
      console.error(err);
      setMessage("Error deleting device");
    }
  };

  const handleAddDevice = () => {
    setAddDeviceName("");
    setShowAddModal(true);
  };

const handleGenerateNewDevice = async () => {
  if (!user || !addDeviceName.trim()) return;

  setLoading(true);
  try {
    // 1️⃣ Begin registration
    const begin = await startRegisterExistingDevice();

    // 2️⃣ Prepare WebAuthn options
    const publicKey = prepareCredentialCreateOptions(begin.options);

    // 3️⃣ Call WebAuthn API
    const credential = (await navigator.credentials.create({ publicKey }));

    // 4️⃣ Convert credential to JSON for backend
    const attestationResponse = attestationToJSON(credential);

    // 5️⃣ Complete registration
    const result = await finishRegister(begin.challengeId, attestationResponse, addDeviceName.trim());

    if (result.success) {
      setDevices(prev => [
        ...prev,
        {
          credentialId: result.credentialId,
          deviceName: addDeviceName.trim(),
          createdAt: new Date().toISOString(),
          lastUsedAt: null,
        },
      ]);
      setShowAddModal(false);
      setAddDeviceName("");
    } else {
      setMessage(result.message || "Failed to add device");
    }
  } catch (err) {
    console.error(err);
    setMessage("Error adding device");
  } finally {
    setLoading(false);
  }
};
  const handleDeleteAccount = async () => {
    if (!window.confirm("Are you sure you want to delete your account?")) return;
    try {
      await deleteAccount();
      logout();
    } catch (err) {
      console.error(err);
      setMessage("Error deleting account");
    }
  };

  const handleLogout = () => {
    logout();
  };

  return (
    <div className="welcome-container modern">
      <div className="header-flex">
        <h1>Security Settings</h1>
        <button className="btn-logout" onClick={handleLogout}>
          <FaSignOutAlt style={{ marginRight: "6px" }} /> Logout
        </button>
      </div>

      {message && <p className="error-message">{message}</p>}

      <table className="device-table modern-table">
        <thead>
          <tr>
            <th>Device</th>
            <th>Rename</th>
            <th>Remove</th>
          </tr>
        </thead>
        <tbody>
          {devices.map(device => (
            <tr key={device.credentialId}>
              <td className="device-cell">
                <FaDesktop style={{ marginRight: "8px" }} />
                {editingId === device.credentialId ? (
                  <input
                    type="text"
                    value={newDeviceName}
                    onChange={e => setNewDeviceName(e.target.value)}
                    onKeyDown={e => {
                      if (e.key === "Enter") handleSaveDeviceName(device.credentialId);
                    }}
                  />
                ) : (
                  <span>{device.deviceName || "Unnamed Device"}</span>
                )}
              </td>
              <td>
                {editingId === device.credentialId ? (
                  <button className="btn-small save" onClick={() => handleSaveDeviceName(device.credentialId)}>
                    <FaSave /> Save
                  </button>
                ) : (
                  <button className="btn-small edit" onClick={() => { setEditingId(device.credentialId); setNewDeviceName(device.deviceName || ""); }}>
                    <FaEdit /> Change
                  </button>
                )}
              </td>
              <td>
                <button className="btn-small delete" onClick={() => handleDeleteDevice(device.credentialId)}>
                  <FaTrash /> Delete
                </button>
              </td>
            </tr>
          ))}
          <tr>
            <td colSpan={3}>
              <button className="btn-add-device" onClick={handleAddDevice}>
                <FaPlus style={{ marginRight: "6px" }} /> Add Another Device
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      {showAddModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>Add New Device</h3>
            <label>Device Name</label>
            <input type="text" value={addDeviceName} onChange={e => setAddDeviceName(e.target.value)} placeholder="Enter device name" />
            <div className="modal-actions">
              <button className="btn-primary" onClick={handleGenerateNewDevice} disabled={loading}>
                {loading ? "Generating..." : "Generate Passkey"}
              </button>
              <button className="btn-secondary" onClick={() => setShowAddModal(false)}>Cancel</button>
            </div>
          </div>
        </div>
      )}

      <div className="delete-account modern-delete">
        <button className="btn-danger" onClick={handleDeleteAccount}>
          DELETE ACCOUNT
        </button>
      </div>
    </div>
  );
}
